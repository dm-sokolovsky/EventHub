using EventHub.Api.Common.Exceptions;
using EventHub.Api.Models.Booking;
using EventHub.Api.Models.Event;
using EventHub.Api.Services;

namespace EventHub.Tests;

[Collection("EventService collection")]
public class BookingServiceTests
{
    private readonly EventService _eventService;
    private readonly BookingService _bookingService;

    public BookingServiceTests(EventServiceFixture fixture)
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
    public async Task CreateBookingAsync_ForExistingEvent_ReturnsPendingBooking()
    {
        var @event = CreateTestEvent($"booking_ok_{Guid.NewGuid()}");

        var booking = await _bookingService.CreateBookingAsync(@event.Id);

        Assert.NotNull(booking);
        Assert.Equal(@event.Id, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Null(booking.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_HaveUniqueIds()
    {
        var @event = CreateTestEvent($"booking_multi_{Guid.NewGuid()}");

        var booking1 = await _bookingService.CreateBookingAsync(@event.Id);
        var booking2 = await _bookingService.CreateBookingAsync(@event.Id);
        var booking3 = await _bookingService.CreateBookingAsync(@event.Id);

        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.NotEqual(booking2.Id, booking3.Id);
        Assert.NotEqual(booking1.Id, booking3.Id);
        Assert.All([booking1, booking2, booking3], b => Assert.Equal(@event.Id, b.EventId));
    }

    [Fact]
    public async Task GetBookingByIdAsync_ReturnsCorrectBooking()
    {
        var @event = CreateTestEvent($"booking_get_{Guid.NewGuid()}");
        var created = await _bookingService.CreateBookingAsync(@event.Id);

        var fetched = await _bookingService.GetBookingByIdAsync(created.Id);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.EventId, fetched.EventId);
        Assert.Equal(created.Status, fetched.Status);
        Assert.Equal(created.CreatedAt, fetched.CreatedAt);
    }

    [Fact]
    public async Task GetBookingByIdAsync_AfterConfirm_ReflectsUpdatedStatus()
    {
        var @event = CreateTestEvent($"booking_confirm_{Guid.NewGuid()}");
        var created = await _bookingService.CreateBookingAsync(@event.Id);

        created.Confirm();

        var fetched = await _bookingService.GetBookingByIdAsync(created.Id);

        Assert.NotNull(fetched);
        Assert.Equal(BookingStatus.Confirmed, fetched.Status);
        Assert.NotNull(fetched.ProcessedAt);
    }

    [Fact]
    public async Task GetBookingByIdAsync_AfterReject_ReflectsUpdatedStatus()
    {
        var @event = CreateTestEvent($"booking_reject_{Guid.NewGuid()}");
        var created = await _bookingService.CreateBookingAsync(@event.Id);

        created.Reject();

        var fetched = await _bookingService.GetBookingByIdAsync(created.Id);

        Assert.NotNull(fetched);
        Assert.Equal(BookingStatus.Rejected, fetched.Status);
        Assert.NotNull(fetched.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_ForNonExistingEvent_ThrowsNotFoundException()
    {
        var nonExistingEventId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _bookingService.CreateBookingAsync(nonExistingEventId));
    }

    [Fact]
    public async Task CreateBookingAsync_ForDeletedEvent_ThrowsNotFoundException()
    {
        var @event = CreateTestEvent($"booking_deleted_{Guid.NewGuid()}");
        _eventService.DeleteEvent(@event.Id);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _bookingService.CreateBookingAsync(@event.Id));
    }

    [Fact]
    public async Task GetBookingByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _bookingService.GetBookingByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateBookingAsync_DecreasesAvailableSeatsByOne()
    {
        var @event = CreateTestEvent($"booking_seats_dec_{Guid.NewGuid()}", totalSeats: 5);

        await _bookingService.CreateBookingAsync(@event.Id);

        Assert.Equal(4, @event.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_UpToCapacity_AllSucceedWithUniqueIds()
    {
        var @event = CreateTestEvent($"booking_seats_capacity_{Guid.NewGuid()}", totalSeats: 3);

        var booking1 = await _bookingService.CreateBookingAsync(@event.Id);
        var booking2 = await _bookingService.CreateBookingAsync(@event.Id);
        var booking3 = await _bookingService.CreateBookingAsync(@event.Id);

        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.NotEqual(booking2.Id, booking3.Id);
        Assert.NotEqual(booking1.Id, booking3.Id);
        Assert.Equal(0, @event.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_AfterSeatsExhausted_ThrowsNoAvailableSeatsException()
    {
        var @event = CreateTestEvent($"booking_seats_exhausted_{Guid.NewGuid()}", totalSeats: 1);
        await _bookingService.CreateBookingAsync(@event.Id);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(
            () => _bookingService.CreateBookingAsync(@event.Id));
    }

    [Fact]
    public async Task CreateBookingAsync_NoAvailableSeats_DoesNotChangeAvailableSeats()
    {
        var @event = CreateTestEvent($"booking_seats_unchanged_{Guid.NewGuid()}", totalSeats: 1);
        await _bookingService.CreateBookingAsync(@event.Id);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(
            () => _bookingService.CreateBookingAsync(@event.Id));

        Assert.Equal(0, @event.AvailableSeats);
    }

    [Fact]
    public void Confirm_SetsStatusConfirmedAndProcessedAt()
    {
        var booking = new Booking(Guid.NewGuid());

        booking.Confirm();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public void Reject_SetsStatusRejectedAndProcessedAt()
    {
        var booking = new Booking(Guid.NewGuid());

        booking.Reject();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public async Task Reject_ThenReleaseSeat_RestoresAvailableSeats()
    {
        var @event = CreateTestEvent($"booking_release_restore_{Guid.NewGuid()}", totalSeats: 2);
        var booking = await _bookingService.CreateBookingAsync(@event.Id);
        Assert.Equal(1, @event.AvailableSeats);

        booking.Reject();
        @event.ReleaseSeat();

        Assert.Equal(2, @event.AvailableSeats);
    }

    [Fact]
    public async Task Reject_ThenReleaseSeat_AllowsNewBookingForSameSeat()
    {
        var @event = CreateTestEvent($"booking_release_new_{Guid.NewGuid()}", totalSeats: 1);
        var firstBooking = await _bookingService.CreateBookingAsync(@event.Id);

        firstBooking.Reject();
        @event.ReleaseSeat();

        var secondBooking = await _bookingService.CreateBookingAsync(@event.Id);

        Assert.NotNull(secondBooking);
        Assert.NotEqual(firstBooking.Id, secondBooking.Id);
        Assert.Equal(0, @event.AvailableSeats);
    }
}
