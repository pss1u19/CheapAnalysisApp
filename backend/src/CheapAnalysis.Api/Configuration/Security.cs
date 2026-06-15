using CheapAnalysis.Api.Middleware;
using FastEndpoints;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CheapAnalysis.Api.Configuration;

/// <summary>
/// Session-cookie authentication, CSRF double-submit, and the development CORS policy (T-101).
/// Registration wires the machinery; <see cref="IssueCsrfCookie"/> emits the script-readable
/// <c>XSRF-TOKEN</c> request-token cookie that the magic-link endpoints call at session issuance
/// in T-102 — the first endpoints to opt into antiforgery validation. See ARCHITECTURE.md §12.
/// </summary>
public static class Security
{
    /// <summary>
    /// Name of the opaque session-id cookie. The <c>__Host-</c> prefix is enforced by the
    /// browser: the cookie is rejected unless it is Secure, has <c>Path=/</c>, and carries no
    /// <c>Domain</c> — a belt-and-braces guard on top of the options set below.
    /// </summary>
    public const string SessionCookieName = "__Host-Session";

    /// <summary>
    /// Non-HttpOnly cookie holding the antiforgery request token. The SPA's csrf interceptor
    /// reads it and echoes the value back in <see cref="CsrfHeaderName"/> on unsafe requests.
    /// </summary>
    public const string CsrfCookieName = "XSRF-TOKEN";

    /// <summary>Header the SPA sets from <see cref="CsrfCookieName"/>; matches Angular's convention.</summary>
    public const string CsrfHeaderName = "X-XSRF-TOKEN";

    /// <summary>
    /// CORS policy applied so the Angular dev server (a different origin) can call the API with
    /// credentials. In production both sit behind one Caddy origin and CORS never engages.
    /// </summary>
    public const string CorsPolicyName = "SpaCors";

    /// <summary>
    /// Configuration key (string array) listing the origins the <see cref="CorsPolicyName"/>
    /// policy trusts. Empty in production (same-origin), <c>http://localhost:4200</c> in dev.
    /// </summary>
    public const string AllowedOriginsKey = "Cors:AllowedOrigins";

    /// <summary>
    /// Registers cookie authentication, authorization, antiforgery, and the SPA CORS policy.
    /// </summary>
    public static IServiceCollection AddSessionSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(cookieOptions =>
            {
                cookieOptions.Cookie.Name = SessionCookieName;
                cookieOptions.Cookie.HttpOnly = true;
                cookieOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                cookieOptions.Cookie.SameSite = SameSiteMode.Strict;
                cookieOptions.Cookie.Path = "/";
                cookieOptions.SlidingExpiration = true;
                cookieOptions.ExpireTimeSpan = TimeSpan.FromDays(14);

                // This API backs an SPA, not server-rendered pages: an unauthenticated or
                // forbidden request must get a status code, never a 302 to a login URL that
                // lives on the Angular side. Without these the cookie handler would redirect.
                cookieOptions.Events.OnRedirectToLogin = SetStatusCode(StatusCodes.Status401Unauthorized);
                cookieOptions.Events.OnRedirectToAccessDenied = SetStatusCode(StatusCodes.Status403Forbidden);
            });

        services.AddAuthorization();

        services.AddAntiforgery(antiforgeryOptions =>
        {
            antiforgeryOptions.HeaderName = CsrfHeaderName;
            // Leave the framework's own antiforgery cookie at its default name and HttpOnly: it
            // carries the *cookie token*, which the browser must never expose to script. The
            // matching *request token* the SPA actually echoes rides a separate, script-readable
            // XSRF-TOKEN cookie that IssueCsrfCookie writes at session issuance.
            //
            // T-101 instead renamed this cookie to XSRF-TOKEN and dropped HttpOnly, so the SPA
            // read the cookie token and echoed it as X-XSRF-TOKEN — ValidateRequestAsync then
            // rejected every unsafe request (cookie token where a request token is required).
            // Arch review F1, fixed in T-112. Transport hardening still applies to this cookie:
            antiforgeryOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            antiforgeryOptions.Cookie.SameSite = SameSiteMode.Strict;
        });

        services.AddCors(corsOptions => corsOptions.AddPolicy(CorsPolicyName, policyBuilder =>
        {
            var allowedOrigins = configuration.GetSection(AllowedOriginsKey).Get<string[]>() ?? [];
            if (allowedOrigins.Length == 0)
            {
                // Same-origin production: trust nothing. Same-origin requests never consult
                // CORS, so an empty policy is the safe default rather than a footgun.
                return;
            }

            policyBuilder
                .WithOrigins(allowedOrigins)
                .AllowCredentials()
                .WithMethods(
                    HttpMethods.Get,
                    HttpMethods.Post,
                    HttpMethods.Put,
                    HttpMethods.Patch,
                    HttpMethods.Delete)
                .WithHeaders(
                    "Content-Type",
                    CsrfHeaderName,
                    IdempotencyMiddleware.IdempotencyKeyHeader);
        }));

        return services;
    }

    /// <summary>
    /// Inserts CORS, authentication, authorization, and antiforgery into the request pipeline
    /// in the order they must run. Call before <c>UseFastEndpoints</c>.
    /// </summary>
    public static WebApplication UseSessionSecurity(this WebApplication application)
    {
        application.UseCors(CorsPolicyName);
        application.UseAuthentication();
        application.UseAuthorization();
        // Validates the header/cookie token pair on unsafe FastEndpoints requests that opt in
        // via EnableAntiforgery(); a no-op for endpoints that don't. T-102 is the first opt-in.
        application.UseAntiforgeryFE();
        return application;
    }

    /// <summary>
    /// Emits the CSRF double-submit pair at session issuance. ASP.NET's antiforgery service stores
    /// the HttpOnly cookie token in its framework cookie; the matching request token is written
    /// here into the script-readable <see cref="CsrfCookieName"/> cookie. The SPA reads that cookie
    /// and echoes the value back in <see cref="CsrfHeaderName"/> on unsafe requests, where
    /// <c>UseAntiforgeryFE</c> validates the pair. T-102 calls this from the magic-link callback
    /// once the session cookie is set; mirrors the response described in ARCHITECTURE.md §12.1.
    /// </summary>
    public static void IssueCsrfCookie(this HttpContext httpContext, IAntiforgery antiforgery)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(antiforgery);

        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        var requestToken = tokens.RequestToken
            ?? throw new InvalidOperationException(
                "Antiforgery produced no request token to issue into the XSRF-TOKEN cookie.");
        httpContext.Response.Cookies.Append(CsrfCookieName, requestToken, new CookieOptions
        {
            // Script-readable on purpose: the SPA copies this into the request header. The
            // separate, HttpOnly cookie token is what makes the double-submit unforgeable.
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
        });
    }

    private static Func<RedirectContext<CookieAuthenticationOptions>, Task> SetStatusCode(int statusCode)
        => redirectContext =>
        {
            redirectContext.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        };
}
