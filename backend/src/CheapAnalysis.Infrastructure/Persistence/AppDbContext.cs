using CheapAnalysis.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CheapAnalysis.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> contextOptions) : DbContext(contextOptions)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
