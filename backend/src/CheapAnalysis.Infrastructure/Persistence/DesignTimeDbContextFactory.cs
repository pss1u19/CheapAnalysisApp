using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CheapAnalysis.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> when adding migrations or generating SQL scripts so the
/// tooling can construct an <see cref="AppDbContext"/> without booting the API host.
/// Connection string here only needs to be parseable — migrations don't connect.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=cheapanalysis;Username=postgres;Password=postgres";

    public AppDbContext CreateDbContext(string[] args)
    {
        var contextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                DesignTimeConnectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(contextOptions);
    }
}
