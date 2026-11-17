using BookingsService.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingsService.Infrastructure.Configurations;

public sealed class LocalEventConfiguration : IEntityTypeConfiguration<LocalEvent>
{
    public void Configure(EntityTypeBuilder<LocalEvent> builder)
    {
        builder.ToTable("cd_local_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.StartAt)
            .HasColumnName("start_at")
            .IsRequired();

        builder.Property(e => e.EndAt)
            .HasColumnName("end_at")
            .IsRequired();

        builder.Property(e => e.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();

        builder.Property(e => e.AvailableSeats)
            .HasColumnName("available_seats")
            .IsRequired();
    }
}
