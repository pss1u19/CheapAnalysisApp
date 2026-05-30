using CheapAnalysis.Infrastructure.Persistence;
using CheapAnalysis.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CheapAnalysis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AppDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing ConnectionStrings:AppDb. Set it in appsettings or via env (ConnectionStrings__AppDb).");
        }

        services.AddSingleton<CurrentUserConnectionInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, contextOptions) =>
        {
            contextOptions.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            contextOptions.AddInterceptors(
                serviceProvider.GetRequiredService<CurrentUserConnectionInterceptor>());
        });

        return services;
    }
}
