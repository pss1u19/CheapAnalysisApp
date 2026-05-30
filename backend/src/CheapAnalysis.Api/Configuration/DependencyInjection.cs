using CheapAnalysis.Infrastructure;
using CheapAnalysis.Infrastructure.Persistence;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace CheapAnalysis.Api.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFastEndpoints();
        services.SwaggerDocument(swaggerOptions =>
        {
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

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(name: "db", tags: ["ready"]);

        return services;
    }
}
