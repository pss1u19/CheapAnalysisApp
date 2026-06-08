using CheapAnalysis.Application.Idempotency;
using CheapAnalysis.Infrastructure.Idempotency;
using CheapAnalysis.Infrastructure.Persistence;
using CheapAnalysis.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

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

        AddIdempotencyStore(services, configuration);

        return services;
    }

    /// <summary>
    /// Wires the Redis-backed idempotency store (T-018) when a Redis connection string is
    /// present. With none configured the store is left unregistered and the API treats
    /// idempotency as disabled — mirroring how Sentry and the DB no-op without their env.
    /// </summary>
    private static void AddIdempotencyStore(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            return;
        }

        var redisConfiguration = ConfigurationOptions.Parse(redisConnectionString);
        // Connect lazily and don't throw at startup if Redis is down, so the host still
        // boots; commands surface failures at request time where the middleware fails open.
        redisConfiguration.AbortOnConnectFail = false;

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConfiguration));
        services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
    }
}
