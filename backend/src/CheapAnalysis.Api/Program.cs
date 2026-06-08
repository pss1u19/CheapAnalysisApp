using CheapAnalysis.Api.Configuration;
using CheapAnalysis.Api.Middleware;
using FastEndpoints;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((hostContext, _, loggerConfig) => loggerConfig
        .ReadFrom.Configuration(hostContext.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "CheapAnalysis.Api")
        .WriteTo.Console(new CompactJsonFormatter()));

    // Sentry binds the "Sentry" config section and the SENTRY_DSN env var (T-017).
    // With no DSN configured the SDK is a no-op, so the stack still runs without it.
    builder.WebHost.UseSentry();

    builder.Services.AddApiServices(builder.Configuration);

    var application = builder.Build();

    application.UseSerilogRequestLogging();
    application.UseExceptionHandler();
    application.UseStatusCodePages();

    // T-018: replay/short-circuit mutating requests carrying an Idempotency-Key. Sits
    // below the exception handler so a thrown endpoint releases the key (see middleware).
    application.UseIdempotency();

    // ShortNames makes swagger operationIds the endpoint class name (PingEndpoint)
    // instead of the full namespace path, so generated client methods read cleanly.
    application.UseFastEndpoints(fastEndpoints => fastEndpoints.Endpoints.ShortNames = true);
    application.UseOpenApiDocs();

    application.MapHealthChecks("/healthz", new HealthCheckOptions
    {
        Predicate = _ => false,
    });
    application.MapHealthChecks("/readyz", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
    });

    application.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "CheapAnalysis.Api host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
