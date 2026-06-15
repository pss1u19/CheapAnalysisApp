using CheapAnalysis.Api.Configuration;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
// Aliased to avoid clashing with Microsoft.AspNetCore.Http.SameSiteMode used by the options below.
using SetCookieHeaderValue = Microsoft.Net.Http.Headers.SetCookieHeaderValue;

namespace CheapAnalysis.IntegrationTests.Configuration;

/// <summary>
/// Verifies the T-101 security wiring registers cookie auth, antiforgery, and CORS with the
/// strict options ARCHITECTURE.md §12.2 specifies, and that the CSRF double-submit pair actually
/// round-trips (T-112): the request token issued into the script-readable XSRF-TOKEN cookie
/// validates when echoed back in the header, while a missing or stale token is rejected.
/// </summary>
public sealed class SessionSecurityTests
{
    [Fact]
    public void Session_cookie_uses_strict_host_prefixed_options()
    {
        using var provider = BuildProvider();

        var cookieOptions = provider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        cookieOptions.Cookie.Name.Should().Be("__Host-Session");
        cookieOptions.Cookie.HttpOnly.Should().BeTrue();
        cookieOptions.Cookie.SecurePolicy.Should().Be(CookieSecurePolicy.Always);
        cookieOptions.Cookie.SameSite.Should().Be(SameSiteMode.Strict);
        cookieOptions.Cookie.Path.Should().Be("/");
    }

    [Fact]
    public async Task Unauthenticated_request_gets_401_instead_of_a_login_redirect()
    {
        using var provider = BuildProvider();
        var cookieOptions = provider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        var statusCode = await CaptureStatusAfter(cookieOptions.Events.OnRedirectToLogin, cookieOptions);

        statusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task Forbidden_request_gets_403_instead_of_an_access_denied_redirect()
    {
        using var provider = BuildProvider();
        var cookieOptions = provider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        var statusCode = await CaptureStatusAfter(cookieOptions.Events.OnRedirectToAccessDenied, cookieOptions);

        statusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void Antiforgery_reads_the_double_submit_header_and_keeps_its_framework_cookie_httponly()
    {
        using var provider = BuildProvider();

        var antiforgeryOptions = provider.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;

        antiforgeryOptions.HeaderName.Should().Be("X-XSRF-TOKEN");
        // The framework cookie holds the cookie token and must stay HttpOnly under its default
        // name. T-101 renamed it to XSRF-TOKEN and exposed it to script (arch review F1); echoing
        // a cookie token as the request-token header fails validation. Guard against regressing.
        antiforgeryOptions.Cookie.HttpOnly.Should().BeTrue();
        antiforgeryOptions.Cookie.Name.Should().NotBe(Security.CsrfCookieName);
        antiforgeryOptions.Cookie.SecurePolicy.Should().Be(CookieSecurePolicy.Always);
        antiforgeryOptions.Cookie.SameSite.Should().Be(SameSiteMode.Strict);
    }

    [Fact]
    public void Issued_cookies_keep_the_framework_token_httponly_and_the_request_token_readable()
    {
        using var provider = BuildProvider();

        var issued = IssueSession(provider);

        var cookies = ReadSetCookies(issued);
        var frameworkCookie = cookies.Single(cookie =>
            cookie.Name.StartsWith(".AspNetCore.Antiforgery", StringComparison.Ordinal));
        var requestTokenCookie = cookies.Single(cookie => cookie.Name == Security.CsrfCookieName);

        frameworkCookie.HttpOnly.Should().BeTrue("the cookie token must never be exposed to script");
        requestTokenCookie.HttpOnly.Should().BeFalse("the SPA must read the request token to echo it");
        requestTokenCookie.Secure.Should().BeTrue();
    }

    [Fact]
    public async Task Issued_request_token_round_trips_from_cookie_to_header()
    {
        using var provider = BuildProvider();
        var antiforgery = provider.GetRequiredService<IAntiforgery>();

        // Session issuance stores the framework cookie token and emits the readable request token;
        // a subsequent unsafe request echoes the cookies and the decoded XSRF-TOKEN value as the
        // header. This is the path T-102's endpoints will exercise.
        var issued = IssueSession(provider);
        var validateContext = BuildEchoingRequest(issued, ReadEchoedRequestToken(issued));

        (await antiforgery.IsRequestValidAsync(validateContext)).Should().BeTrue();
    }

    [Fact]
    public async Task Request_without_the_token_header_is_rejected()
    {
        using var provider = BuildProvider();
        var antiforgery = provider.GetRequiredService<IAntiforgery>();

        // Cookies echoed, but no X-XSRF-TOKEN header — UseAntiforgeryFE surfaces this as a 400.
        var validateContext = BuildEchoingRequest(IssueSession(provider), headerToken: null);

        await antiforgery.Invoking(service => service.ValidateRequestAsync(validateContext))
            .Should().ThrowAsync<AntiforgeryValidationException>();
    }

    [Fact]
    public async Task Request_with_a_stale_token_header_is_rejected()
    {
        using var provider = BuildProvider();
        var antiforgery = provider.GetRequiredService<IAntiforgery>();

        var issued = IssueSession(provider);
        // A second, independent issuance yields a request token that does not match this session's
        // cookie token — the mismatched pair models a stale token the SPA might replay.
        var staleToken = ReadEchoedRequestToken(IssueSession(provider));

        var validateContext = BuildEchoingRequest(issued, staleToken);

        await antiforgery.Invoking(service => service.ValidateRequestAsync(validateContext))
            .Should().ThrowAsync<AntiforgeryValidationException>();
    }

    [Fact]
    public void Cors_policy_trusts_configured_origins_with_credentials()
    {
        using var provider = BuildProvider();

        var policy = provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(Security.CorsPolicyName);

        policy.Should().NotBeNull();
        policy!.Origins.Should().ContainSingle().Which.Should().Be("http://localhost:4200");
        policy.SupportsCredentials.Should().BeTrue();
        policy.Headers.Should().Contain("X-XSRF-TOKEN");
    }

    [Fact]
    public void Cors_policy_trusts_nothing_when_no_origins_are_configured()
    {
        using var provider = BuildProvider(corsOrigins: []);

        var policy = provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(Security.CorsPolicyName);

        policy.Should().NotBeNull();
        policy!.Origins.Should().BeEmpty();
    }

    // null => the dev default of a single trusted origin; an explicit [] => none configured.
    private static ServiceProvider BuildProvider(string[]? corsOrigins = null)
    {
        var origins = corsOrigins ?? ["http://localhost:4200"];
        var configurationValues = new Dictionary<string, string?>();
        for (var index = 0; index < origins.Length; index++)
        {
            configurationValues[$"{Security.AllowedOriginsKey}:{index}"] = origins[index];
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configurationValues).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSessionSecurity(configuration);
        // Antiforgery protects its tokens with data protection; an ephemeral provider keeps the
        // round-trip self-contained (no key ring on disk) while issue and validate share one key.
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        return services.BuildServiceProvider();
    }

    // Runs the session-issuance step over HTTPS (SecurePolicy.Always rejects plain HTTP, as it
    // should) and returns the response carrying the framework cookie token + readable XSRF-TOKEN.
    private static HttpResponse IssueSession(IServiceProvider provider)
    {
        var context = new DefaultHttpContext { RequestServices = provider, Request = { IsHttps = true } };
        context.IssueCsrfCookie(provider.GetRequiredService<IAntiforgery>());
        return context.Response;
    }

    // Replays a freshly issued response back as the next request, the way a browser would: the
    // Set-Cookie values become the Cookie header, and the SPA's decoded request token (if any)
    // becomes the X-XSRF-TOKEN header.
    private static DefaultHttpContext BuildEchoingRequest(HttpResponse issued, string? headerToken)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = issued.HttpContext.RequestServices,
            Request = { IsHttps = true, Method = HttpMethods.Post },
        };
        context.Request.Headers.Cookie = string.Join(
            "; ",
            ReadSetCookies(issued).Select(cookie => $"{cookie.Name}={cookie.Value}"));
        if (headerToken is not null)
        {
            context.Request.Headers[Security.CsrfHeaderName] = headerToken;
        }

        return context;
    }

    // The SPA reads XSRF-TOKEN via document.cookie, which URL-decodes the value (Angular's
    // parseCookieValue → decodeURIComponent), then echoes that raw token in the header.
    private static string ReadEchoedRequestToken(HttpResponse issued)
    {
        var requestTokenCookie = ReadSetCookies(issued).Single(cookie => cookie.Name == Security.CsrfCookieName);
        return Uri.UnescapeDataString(requestTokenCookie.Value.ToString());
    }

    private static IList<SetCookieHeaderValue> ReadSetCookies(HttpResponse response)
        => SetCookieHeaderValue.ParseList(response.Headers.SetCookie.Select(value => value!).ToList());

    private static async Task<int> CaptureStatusAfter(
        Func<RedirectContext<CookieAuthenticationOptions>, Task> handler,
        CookieAuthenticationOptions cookieOptions)
    {
        var httpContext = new DefaultHttpContext();
        var scheme = new AuthenticationScheme(
            CookieAuthenticationDefaults.AuthenticationScheme,
            displayName: null,
            handlerType: typeof(CookieAuthenticationHandler));
        var redirectContext = new RedirectContext<CookieAuthenticationOptions>(
            httpContext,
            scheme,
            cookieOptions,
            new AuthenticationProperties(),
            redirectUri: "/login");

        await handler(redirectContext);

        return httpContext.Response.StatusCode;
    }
}
