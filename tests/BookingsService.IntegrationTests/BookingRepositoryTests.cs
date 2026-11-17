using BookingsService.Domain.Models;
using BookingsService.Infrastructure;
using BookingsService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BookingsService.IntegrationTests;

[Collection("RepositoryCollection")]
public class BookingRepositoryTests(PostgresFixture fixture)
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.Postgres.GetConnectionString())
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
    public async Task AddAsync_SavesBookingToDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var booking = Booking.CreatePending(Guid.NewGuid(), Guid.NewGuid(), activeCount: 0);

        // Act
        var repository = new BookingRepository(context);
        await repository.AddAsync(booking);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id);
        Assert.NotNull(saved);
        Assert.Equal(BookingStatus.Pending, saved.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectBooking()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var booking = Booking.CreatePending(Guid.NewGuid(), Guid.NewGuid(), activeCount: 0);
        await context.AddAsync(booking);
        await context.SaveChangesAsync();

        // Act
        var repository = new BookingRepository(CreateContext());
        var result = await repository.GetByIdAsync(booking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
    }

    [Fact]
    public async Task GetPendingIds_ReturnsOnlyPendingBookings()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var pending = Booking.CreatePending(eventId, userId, activeCount: 0);
        var confirmed = Booking.CreatePending(eventId, userId, activeCount: 1);
        confirmed.Confirm();
        var cancelled = Booking.CreatePending(eventId, userId, activeCount: 2);
        cancelled.Cancel();
        await context.AddAsync(pending);
        await context.AddAsync(confirmed);
        await context.AddAsync(cancelled);
        await context.SaveChangesAsync();

        // Act
        var repository = new BookingRepository(CreateContext());
        var result = await repository.GetPendingIds();

        // Assert
        Assert.Single(result);
        Assert.Equal(pending.Id, result[0]);
    }

    [Fact]
    public async Task CountActiveBookingsAsync_CountsPendingAndConfirmed()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var pending = Booking.CreatePending(eventId, userId, activeCount: 0);
        var confirmed = Booking.CreatePending(eventId, userId, activeCount: 1);
        confirmed.Confirm();
        var cancelled = Booking.CreatePending(eventId, userId, activeCount: 2);
        cancelled.Cancel();
        await context.AddAsync(pending);
        await context.AddAsync(confirmed);
        await context.AddAsync(cancelled);
        await context.SaveChangesAsync();

        // Act
        var repository = new BookingRepository(CreateContext());
        var count = await repository.CountActiveBookingsAsync(userId);

        // Assert
        Assert.Equal(2, count);
    }
}
