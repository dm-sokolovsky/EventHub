using EventHab.Application.Contracts;
using EventHab.Application.Repositories;
using EventHab.Application.Services;
using EventHab.Application.Services.Abstractions;
using EventHub.Api.Common;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Models;
using EventHub.Infrastructure;
using EventHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Tests;

public sealed class BookingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;
    private readonly ICurrentUser _currentUser;

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddHttpContextAccessor();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
        _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();
        _currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();
        
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    private async Task<Guid> CreateTestEventAsync(int totalSeats = 10)
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var created = await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Test Event",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = totalSeats
        });
        return created.Id;
    }

    #region CreateBookingAsync Tests

    [Fact]
    public async Task CreateBookingAsync_WithValidEventId_ReturnsBookingInfoWithPendingStatus()
    {
        var eventId = await CreateTestEventAsync();
        var userId =  Guid.NewGuid();
        var result = await _bookingService.CreateBookingAsync(eventId, userId);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Null(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_WithValidEventId_SetsCreatedAt()
    {
        var eventId = await CreateTestEventAsync();
        var userId =  Guid.NewGuid();
        var before = DateTime.UtcNow;

        var result = await _bookingService.CreateBookingAsync(eventId, userId);

        var after = DateTime.UtcNow;
        Assert.InRange(result.CreatedAt, before, after);
    }

    [Fact]
    public async Task CreateBookingAsync_WithNonExistentEvent_ThrowsNotFoundException()
    {
        var invalidEventId = Guid.NewGuid();
        var userId =  Guid.NewGuid();
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _bookingService.CreateBookingAsync(invalidEventId, userId));
        Assert.Equal("Event not found", exception.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_AllCreatedWithUniqueIds()
    {
        var eventId = await CreateTestEventAsync(totalSeats: 5);
        var userId =  Guid.NewGuid();
        var results = new List<BookingInfo>();
        for (int i = 0; i < 5; i++)
            results.Add((BookingInfo)await _bookingService.CreateBookingAsync(eventId, userId));

        var uniqueIds = results.Select(r => r.Id).Distinct();
        Assert.Equal(5, uniqueIds.Count());
    }

    [Fact]
    public async Task CreateBookingAsync_WhenNoSeatsAvailable_ThrowsNoAvailableSeatsException()
    {
        var eventId = await CreateTestEventAsync(totalSeats: 1);
        var userId =  Guid.NewGuid();
        await _bookingService.CreateBookingAsync(eventId,userId);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(
            () => _bookingService.CreateBookingAsync(eventId,userId));
    }

    [Fact]
    public async Task CreateBookingAsync_DecrementsAvailableSeats()
    {
        var eventId = await CreateTestEventAsync(totalSeats: 3);
        var userId =  Guid.NewGuid();
        
        await _bookingService.CreateBookingAsync(eventId, userId);
        await _bookingService.CreateBookingAsync(eventId, userId);

        var eventInfo = await _eventService.GetEventByIdAsync(eventId);
        Assert.Equal(1, eventInfo.AvailableSeats);
    }

    #endregion

    #region GetBookingByIdAsync Tests

    [Fact]
    public async Task GetBookingByIdAsync_WithValidId_ReturnsCorrectBookingInfo()
    {
        var eventId = await CreateTestEventAsync();
        var userId =  Guid.NewGuid();
        var created = await _bookingService.CreateBookingAsync(eventId, userId);

        var result = await _bookingService.GetBookingByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.EventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithNonExistentId_ThrowsNotFoundException()
    {
        var invalidId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _bookingService.GetBookingByIdAsync(invalidId));
        Assert.Equal("Booking not found", exception.Message);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_DoesNotOverbookEvent()
    {
        const int totalSeats = 5;
        const int concurrentRequests = 20;
        var eventId = await CreateTestEventAsync(totalSeats: totalSeats);
        var userId =  Guid.NewGuid();

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                try
                {
                    await bookingService.CreateBookingAsync(eventId, userId);
                    return true;
                }
                catch (NoAvailableSeatsException)
                {
                    return false;
                }
            }));

        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => r);
        Assert.Equal(totalSeats, successCount);
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_AllSuccessfulBookingsHaveUniqueIds()
    {
        const int totalSeats = 10;
        const int concurrentRequests = 10;
        var eventId = await CreateTestEventAsync(totalSeats: totalSeats);
        var userId =  Guid.NewGuid();
        var bookingIds = new System.Collections.Concurrent.ConcurrentBag<Guid>();

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                var booking = await bookingService.CreateBookingAsync(eventId, userId);
                bookingIds.Add(booking.Id);
            }));

        await Task.WhenAll(tasks);

        Assert.Equal(totalSeats, bookingIds.Distinct().Count());
    }

    #endregion
    
    
    [Fact]
    public async Task CreateBookingAsync_WithPastEvent_ThrowsValidationException()
    {
        // Arrange
        var eventId = await CreateTestEventAsync();
        var userId = Guid.NewGuid();

        var db = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventEntity = await db.Set<Event>().FindAsync(eventId);

        Assert.NotNull(eventEntity);

        var pastDate = DateTime.UtcNow.AddDays(-2);

        db.Entry(eventEntity).Property(nameof(Event.StartAt))
            .CurrentValue = pastDate;

        db.Entry(eventEntity).Property(nameof(Event.EndAt))
            .CurrentValue = pastDate.AddHours(2);

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // Act
        var act = () => _bookingService.CreateBookingAsync(eventId, userId);

        // Assert
        await Assert.ThrowsAsync<EventAlreadyStartedException>(act);
    }

    
    [Fact]
    public async Task CreateBookingAsync_WhenActiveBookingLimitReached_ThrowsValidationException()
    {
        // Arrange
        const int maxActiveBookings = BookingRules.MaxActiveBookingsPerUser;
        var userId = Guid.NewGuid();

        for (int i = 0; i < maxActiveBookings; i++)
        {
            var eventId = await CreateTestEventAsync();
            await _bookingService.CreateBookingAsync(eventId, userId);
        }

        var newEventId = await CreateTestEventAsync();

        // Act
        var act = () => _bookingService.CreateBookingAsync(newEventId, userId);

        // Assert
        await Assert.ThrowsAsync<BookingLimitExceededException>(act);

        var eventInfo = await _eventService.GetEventByIdAsync(newEventId);
        Assert.Equal(eventInfo.TotalSeats, eventInfo.AvailableSeats);
    }

    
    [Fact]
    public async Task CreateBookingAsync_DifferentUsers_HaveIndependentBookingLimits()
    {
        // Arrange
        const int maxActiveBookings = BookingRules.MaxActiveBookingsPerUser;

        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        for (int i = 0; i < maxActiveBookings; i++)
        {
            var eventId = await CreateTestEventAsync();
            await _bookingService.CreateBookingAsync(eventId, firstUserId);
        }

        var newEventId = await CreateTestEventAsync();

        // Act
        await Assert.ThrowsAsync<BookingLimitExceededException>(
            () => _bookingService.CreateBookingAsync(newEventId, firstUserId));

        var secondUserBooking = await _bookingService
            .CreateBookingAsync(newEventId, secondUserId);

        // Assert
        Assert.NotNull(secondUserBooking);
        Assert.NotEqual(Guid.Empty, secondUserBooking.Id);
        Assert.Equal(newEventId, secondUserBooking.EventId);
        Assert.Equal(BookingStatus.Pending, secondUserBooking.Status);
    }

}
