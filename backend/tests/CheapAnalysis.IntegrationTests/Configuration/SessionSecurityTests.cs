using CheapAnalysis.Api.Configuration;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CheapAnalysis.IntegrationTests.Configuration;

/// <summary>
/// Verifies the T-101 security wiring registers cookie auth, antiforgery, and CORS with the
/// strict options ARCHITECTURE.md §12.2 specifies. Asserting the resolved options is the
/// mechanism check: the contract is the configuration itself, since no endpoints consume it
/// until T-102.
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
    public void Antiforgery_uses_the_double_submit_header_and_a_script_readable_cookie()
    {
        using var provider = BuildProvider();

        var antiforgeryOptions = provider.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;

        antiforgeryOptions.HeaderName.Should().Be("X-XSRF-TOKEN");
        antiforgeryOptions.Cookie.Name.Should().Be("XSRF-TOKEN");
        antiforgeryOptions.Cookie.HttpOnly.Should().BeFalse();
        antiforgeryOptions.Cookie.SecurePolicy.Should().Be(CookieSecurePolicy.Always);
        antiforgeryOptions.Cookie.SameSite.Should().Be(SameSiteMode.Strict);
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
        return services.BuildServiceProvider();
    }

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
