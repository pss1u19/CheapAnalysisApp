using CheapAnalysis.Domain.Users;
using CheapAnalysis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CheapAnalysis.IntegrationTests.Persistence;

public sealed class AppDbContextModelTests
{
    private static AppDbContext CreateContext()
    {
        var contextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time;Username=u;Password=p")
            .Options;
        return new AppDbContext(contextOptions);
    }

    [Fact]
    public void User_is_mapped_to_users_table_with_snake_case_columns()
    {
        using var dbContext = CreateContext();

        var userEntity = dbContext.Model.FindEntityType(typeof(User))!;

        userEntity.GetTableName().Should().Be("users");

        var columnNames = userEntity.GetProperties()
            .Select(property => property.GetColumnName())
            .ToArray();

        columnNames.Should().BeEquivalentTo(["id", "email", "display_name", "created_at", "updated_at"]);
    }

    [Fact]
    public void User_email_has_a_unique_index()
    {
        using var dbContext = CreateContext();

        var userEntity = dbContext.Model.FindEntityType(typeof(User))!;

        var emailIndex = userEntity.GetIndexes()
            .Single(index => index.Properties.Any(property => property.Name == nameof(User.Email)));

        emailIndex.IsUnique.Should().BeTrue();
        emailIndex.GetDatabaseName().Should().Be("ix_users_email");
    }

    [Fact]
    public void Timestamp_columns_use_timestamptz()
    {
        using var dbContext = CreateContext();

        var userEntity = dbContext.Model.FindEntityType(typeof(User))!;

        var createdAtType = userEntity.GetProperty(nameof(User.CreatedAt)).GetColumnType();
        var updatedAtType = userEntity.GetProperty(nameof(User.UpdatedAt)).GetColumnType();

        createdAtType.Should().Be("timestamptz");
        updatedAtType.Should().Be("timestamptz");
    }

    [Fact]
    public void Email_is_required_and_capped_at_320_chars()
    {
        using var dbContext = CreateContext();

        var emailProperty = dbContext.Model
            .FindEntityType(typeof(User))!
            .GetProperty(nameof(User.Email));

        emailProperty.IsNullable.Should().BeFalse();
        emailProperty.GetMaxLength().Should().Be(320);
    }

    [Fact]
    public void Initial_migration_creates_users_table()
    {
        using var dbContext = CreateContext();

        var migrations = dbContext.Database.GetMigrations().ToArray();

        migrations.Should().ContainSingle(migrationId => migrationId.EndsWith("InitialCreate", StringComparison.Ordinal));
    }
}
