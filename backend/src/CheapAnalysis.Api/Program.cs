using CheapAnalysis.Api.Configuration;
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

    builder.Services.AddApiServices(builder.Configuration);

    var application = builder.Build();

    application.UseSerilogRequestLogging();
    application.UseExceptionHandler();
    application.UseStatusCodePages();

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
