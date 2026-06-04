using FastEndpoints;

namespace CheapAnalysis.Api.Endpoints;

/// <summary>
/// Development-only endpoint that throws on purpose so Sentry error capture (T-017)
/// can be verified end to end. Responds 404 outside Development so it never ships live.
/// </summary>
public sealed class SentryTestEndpoint(IWebHostEnvironment environment) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/v1/diagnostics/sentry-test");
        AllowAnonymous();
        Description(routeBuilder => routeBuilder.WithTags("Diagnostics"));
    }

    // Parameter name must match the base (FastEndpoints declares `ct`) — CA1725.
    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!environment.IsDevelopment())
        {
            await SendNotFoundAsync(ct);
            return;
        }

        throw new InvalidOperationException(
            "Sentry verification (T-017): this exception is thrown on purpose.");
    }
}
