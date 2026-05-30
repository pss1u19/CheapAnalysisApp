using FastEndpoints.Swagger;

namespace CheapAnalysis.Api.Configuration;

public static class OpenApi
{
    public static IApplicationBuilder UseOpenApiDocs(this WebApplication application)
    {
        application.UseSwaggerGen();
        return application;
    }
}
