using BookingsService.Application.Contracts;
using BookingsService.Application.Repositories;
using BookingsService.Application.Services;
using BookingsService.Application.Services.Abstractions;
using BookingsService.Domain.Exceptions;
using BookingsService.Domain.Models;
using BookingsService.Infrastructure;
using BookingsService.Infrastructure.Gateways;
using BookingsService.Infrastructure.Models;
using BookingsService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookingsService.Tests;

public sealed class BookingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IBookingService _bookingService;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly FakeCurrentUser _currentUser;

    private static readonly Guid TestEventId = Guid.NewGuid();

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddSingleton<IEventsGateway, LocalEventsGateway>();
        services.AddSingleton<ICurrentUser, FakeCurrentUser>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IBookingService, BookingService>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();
        _dbContextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        _currentUser = (FakeCurrentUser)_serviceProvider.GetRequiredService<ICurrentUser>();

        AddLocalEvent(TestEventId, DateTime.UtcNow.AddDays(1), totalSeats: 10);
    }

    private void AddLocalEvent(Guid eventId, DateTime startAt, int totalSeats)
    {
        using var db = _dbContextFactory.CreateDbContext();
        db.LocalEvents.Add(new LocalEvent
        {
            Id = eventId,
            Title = "Test event",
            StartAt = startAt,
            EndAt = startAt.AddHours(2),
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        });
        db.SaveChanges();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    #region CreateBookingAsync

    [Fact]
    public async Task CreateBookingAsync_WithValidEventId_ReturnsBookingInfoWithPendingStatus()
    {
        var userId = Guid.NewGuid();
        var result = await _bookingService.CreateBookingAsync(TestEventId, userId);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(TestEventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Null(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_WithUnknownEventId_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _bookingService.CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateBookingAsync_WithAlreadyStartedEvent_ThrowsEventAlreadyStartedException()
    {
        var startedEventId = Guid.NewGuid();
        AddLocalEvent(startedEventId, DateTime.UtcNow.AddHours(-1), totalSeats: 10);

        await Assert.ThrowsAsync<EventAlreadyStartedException>(() =>
            _bookingService.CreateBookingAsync(startedEventId, Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateBookingAsync_WithoutAvailableSeats_ThrowsNoAvailableSeatsException()
    {
        var smallEventId = Guid.NewGuid();
        AddLocalEvent(smallEventId, DateTime.UtcNow.AddDays(1), totalSeats: 1);

        await _bookingService.CreateBookingAsync(smallEventId, Guid.NewGuid());

        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _bookingService.CreateBookingAsync(smallEventId, Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateBookingAsync_WhenUserLimitExceeded_ThrowsBookingLimitExceededException()
    {
        var userId = Guid.NewGuid();

        for (var i = 0; i < BookingRules.MaxActiveBookingsPerUser; i++)
        {
            var eventId = Guid.NewGuid();
            AddLocalEvent(eventId, DateTime.UtcNow.AddDays(1), totalSeats: 100);
            await _bookingService.CreateBookingAsync(eventId, userId);
        }

        var oneMoreEventId = Guid.NewGuid();
        AddLocalEvent(oneMoreEventId, DateTime.UtcNow.AddDays(1), totalSeats: 100);

        await Assert.ThrowsAsync<BookingLimitExceededException>(() =>
            _bookingService.CreateBookingAsync(oneMoreEventId, userId));
    }

    #endregion

    #region GetBookingByIdAsync

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingId_ReturnsBookingInfo()
    {
        var created = await _bookingService.CreateBookingAsync(TestEventId, Guid.NewGuid());

        var result = await _bookingService.GetBookingByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.EventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithUnknownId_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _bookingService.GetBookingByIdAsync(Guid.NewGuid()));
    }

    #endregion

    #region CancelledBookingByIdAsync

    [Fact]
    public async Task CancelledBookingByIdAsync_ByOwner_SetsCancelledStatus()
    {
        var userId = Guid.NewGuid();
        _currentUser.Id = userId;
        var created = await _bookingService.CreateBookingAsync(TestEventId, userId);

        var result = await _bookingService.CancelledBookingByIdAsync(created.Id);

        Assert.Equal(BookingStatus.Cancelled, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task CancelledBookingByIdAsync_ByAnotherUser_ThrowsBookingAccessDeniedException()
    {
        var ownerId = Guid.NewGuid();
        var created = await _bookingService.CreateBookingAsync(TestEventId, ownerId);

        _currentUser.Id = Guid.NewGuid();
        _currentUser.IsAdmin = false;

        await Assert.ThrowsAsync<BookingAccessDeniedException>(() =>
            _bookingService.CancelledBookingByIdAsync(created.Id));
    }

    [Fact]
    public async Task CancelledBookingByIdAsync_ByAdmin_SetsCancelledStatus()
    {
        var ownerId = Guid.NewGuid();
        var created = await _bookingService.CreateBookingAsync(TestEventId, ownerId);

        _currentUser.Id = Guid.NewGuid();
        _currentUser.IsAdmin = true;

        var result = await _bookingService.CancelledBookingByIdAsync(created.Id);

        Assert.Equal(BookingStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task CancelledBookingByIdAsync_AlreadyCancelled_ThrowsInvalidBookingStateException()
    {
        var userId = Guid.NewGuid();
        _currentUser.Id = userId;
        var created = await _bookingService.CreateBookingAsync(TestEventId, userId);

        await _bookingService.CancelledBookingByIdAsync(created.Id);

        await Assert.ThrowsAsync<InvalidBookingStateException>(() =>
            _bookingService.CancelledBookingByIdAsync(created.Id));
    }

    #endregion
}
