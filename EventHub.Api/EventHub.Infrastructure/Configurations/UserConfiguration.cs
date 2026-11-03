using EventHub.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Configurations;

public sealed class UserConfiguration: IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id");
        
        builder.Property(user => user.Login)
            .HasColumnName("login")
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();
        
        builder.Property(user => user.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .IsRequired();
        
        builder.HasIndex(user => user.Login)
            .IsUnique();
    }
}