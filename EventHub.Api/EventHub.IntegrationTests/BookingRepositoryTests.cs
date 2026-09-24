using EventHub.Api.DataAccess;
using EventHub.Api.DataAccess.Repositories;
using EventHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace EventHub.IntegrationTests;

public class BookingRepositoryTests : IAsyncLifetime
{
    
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eventhub_tests")
        .Build();
    
    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }
    
    
    [Fact]
    public async Task CreateBooking_NotFoundEvent_ThrowsDbUpdateException()
    {
        await ResetDatabaseAsync();
        
        // Arrange
        await using var context = CreateContext();
        var repo = new BookingRepository(context);
        var booking = Booking.CreatePending(Guid.NewGuid());

        // Act


        // Assert 
        await Assert.ThrowsAsync<DbUpdateException>(() => repo.AddAsync(booking));
    } 
    
    [Fact]
    public async Task CreateBooking_SaveBookingToDatabase()
    {
        await ResetDatabaseAsync();
        
        // Arrange
        await using var contextEvent = CreateContext();
        await using var contextBooking = CreateContext();
        var repoEvent = new EventRepository(contextEvent);
        var repoBooking = new BookingRepository(contextBooking);
        var now = DateTime.UtcNow.AddHours(1);
        var eventData = Event.Create("Event1", now, now.AddHours(2), 10);
        var booking = Booking.CreatePending(eventData.Id);

        // Act
        await repoEvent.AddAsync(eventData);
        await repoEvent.SaveChangesAsync();
        
        await repoBooking.AddAsync(booking);
        await repoBooking.SaveChangesAsync();
        

        // Assert 
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings.FirstOrDefaultAsync(b => b.EventId == eventData.Id);
        var countBooking = await verifyContext.Bookings.CountAsync(b => b.EventId == eventData.Id);
        Assert.NotNull(saved);
        Assert.Equal(1, countBooking);
    } 
    
    [Fact]
    public async Task GetById_ReturnsCorrectBooking()
    {
        await ResetDatabaseAsync();
        
        // Arrange
        await using var contextEvent = CreateContext();
        await using var contextBooking = CreateContext();
        var repoEvent = new EventRepository(contextEvent);
        var repoBooking = new BookingRepository(contextBooking);
        var now = DateTime.UtcNow.AddHours(1);
        var eventData = Event.Create("Event1", now, now.AddHours(2), 10);
        var booking = Booking.CreatePending(eventData.Id);

        // Act
        await repoEvent.AddAsync(eventData);
        await repoEvent.SaveChangesAsync();
        
        await repoBooking.AddAsync(booking);
        await repoBooking.SaveChangesAsync();
        

        // Assert 
        await using var verifyContext = CreateContext();
        var result = await verifyContext.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id);
        Assert.NotNull(result);
    } 
    
    [Fact]
    public async Task GetPendingIds_ReturnsCoollectionIds()
    {
        await ResetDatabaseAsync();
        
        // Arrange
        await using var contextEvent = CreateContext();
        await using var contextBooking = CreateContext();
        var repoEvent = new EventRepository(contextEvent);
        var repoBooking = new BookingRepository(contextBooking);
        var now = DateTime.UtcNow.AddHours(1);
        var eventData = Event.Create("Event1", now, now.AddHours(2), 10);
        var booking1 = Booking.CreatePending(eventData.Id);
        var booking2 = Booking.CreatePending(eventData.Id);

        // Act
        await repoEvent.AddAsync(eventData);
        await repoEvent.SaveChangesAsync();
        
        await repoBooking.AddAsync(booking1);
        await repoBooking.AddAsync(booking2);
        await repoBooking.SaveChangesAsync();
        

        // Assert 
        await using var verifyContext = CreateContext();
        var results = await verifyContext.Bookings.Where(b => b.EventId == eventData.Id).ToListAsync();
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
    } 
}