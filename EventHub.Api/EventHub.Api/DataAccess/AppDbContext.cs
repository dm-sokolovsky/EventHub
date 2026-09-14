using EventHub.Api.Models.Booking;
using EventHub.Api.Models.Event;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.DataAccess;

internal sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : 
        base(options) { }
    
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}