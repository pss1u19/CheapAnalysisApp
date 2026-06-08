using CheapAnalysis.Infrastructure;
using CheapAnalysis.Infrastructure.Persistence;
using FastEndpoints;
using FastEndpoints.Swagger;
// Alias to disambiguate from FastEndpoints' own IdempotencyOptions type (T-018).
using IdempotencyOptions = CheapAnalysis.Api.Middleware.IdempotencyOptions;

namespace CheapAnalysis.Api.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFastEndpoints();
        services.SwaggerDocument(swaggerOptions =>
        {
            // Name schemas after the class (PingResponse) rather than the full
            // namespace path (CheapAnalysisApiEndpointsPingResponse) so generated
            // clients get readable DTO type names.
            swaggerOptions.ShortSchemaNames = true;
            swaggerOptions.DocumentSettings = documentSettings =>
            {
                documentSettings.Title = "CheapAnalysis API";
                documentSettings.Version = "v1";
            };
        });

        services.AddProblemDetails(problemDetailsOptions =>
        {
            problemDetailsOptions.CustomizeProblemDetails = problemContext =>
            {
                problemContext.ProblemDetails.Extensions["traceId"] = problemContext.HttpContext.TraceIdentifier;
            };
        });

        services.AddInfrastructureServices(configuration);

        // T-101: cookie auth scheme + antiforgery (CSRF) + SPA CORS. Sessions and the
        // endpoints that issue them land in T-102.
        services.AddSessionSecurity(configuration);

        // T-018: idempotency is only viable with a Redis store, so force it off when none
        // is configured (the Infrastructure layer leaves IIdempotencyStore unregistered).
        services.AddOptions<IdempotencyOptions>()
            .Bind(configuration.GetSection(IdempotencyOptions.SectionName))
            .PostConfigure(idempotencyOptions =>
            {
                var redisConfigured = !string.IsNullOrWhiteSpace(configuration.GetConnectionString("Redis"));
                idempotencyOptions.Enabled = idempotencyOptions.Enabled && redisConfigured;
            });

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(name: "db", tags: ["ready"]);

        return services;
    }
}
