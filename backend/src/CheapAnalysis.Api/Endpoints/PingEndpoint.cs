using FastEndpoints;

namespace CheapAnalysis.Api.Endpoints;

public sealed class PingEndpoint : EndpointWithoutRequest<PingResponse>
{
    public override void Configure()
    {
        Get("/v1/ping");
        AllowAnonymous();
        Description(routeBuilder => routeBuilder.WithTags("Diagnostics"));
    }

    // Parameter name must match the base (FastEndpoints declares `ct`) — CA1725.
    public override Task HandleAsync(CancellationToken ct)
        => SendAsync(new PingResponse("pong"), cancellation: ct);
}

public sealed record PingResponse(string Message);
