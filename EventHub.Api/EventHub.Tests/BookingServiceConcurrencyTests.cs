using EventHub.Api.Common.Exceptions;
using EventHub.Api.Models.Event;
using EventHub.Api.Services;

namespace EventHub.Tests;

[Collection("EventService collection")]
public class BookingServiceConcurrencyTests
{
    private readonly EventService _eventService;
    private readonly BookingService _bookingService;

    public BookingServiceConcurrencyTests(EventServiceFixture fixture)
    {
        _eventService = fixture.EventService;
        _bookingService = new BookingService(_eventService);
    }

    private Event CreateTestEvent(string title, int totalSeats = 10)
    {
        var @event = new Event(title, "desc", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), totalSeats);
        _eventService.CreateEvent(@event);
        return @event;
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequestsExceedingCapacity_PreventsOverbooking()
    {
        var @event = CreateTestEvent($"booking_concurrency_overbooking_{Guid.NewGuid()}", totalSeats: 5);

        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(async () =>
        {
            try
            {
                await _bookingService.CreateBookingAsync(@event.Id);
                return true;
            }
            catch (NoAvailableSeatsException)
            {
                return false;
            }
        }));

        var results = await Task.WhenAll(tasks);

        Assert.Equal(5, results.Count(success => success));
        Assert.Equal(15, results.Count(success => !success));
        Assert.Equal(0, @event.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequestsWithinCapacity_AllHaveUniqueIds()
    {
        var @event = CreateTestEvent($"booking_concurrency_unique_ids_{Guid.NewGuid()}", totalSeats: 10);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => _bookingService.CreateBookingAsync(@event.Id)));

        var bookings = await Task.WhenAll(tasks);

        Assert.Equal(10, bookings.Length);
        Assert.Equal(10, bookings.Select(b => b.Id).Distinct().Count());
        Assert.Equal(0, @event.AvailableSeats);
    }
}
