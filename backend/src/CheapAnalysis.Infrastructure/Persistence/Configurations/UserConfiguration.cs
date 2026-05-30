using CheapAnalysis.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CheapAnalysis.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> userEntity)
    {
        userEntity.ToTable("users");

        userEntity.HasKey(user => user.Id);

        userEntity.Property(user => user.Id)
            .HasColumnName("id");

        userEntity.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        userEntity.Property(user => user.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(256);

        userEntity.Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        userEntity.Property(user => user.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        userEntity.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email");
    }
}
