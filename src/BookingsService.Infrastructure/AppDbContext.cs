using BookingsService.Domain.Models;
using BookingsService.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingsService.Infrastructure;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) :
        base(options) { }

    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<LocalEvent> LocalEvents => Set<LocalEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
