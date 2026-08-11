using EventHub.Api.Models.Booking;

namespace EventHub.Api.Services;

public class BookingService : IBookingService
{

    private static List<Booking> Bookings { get; } = [];

    public Task CreateBookingAsync(Guid eventId)
    {
        Bookings.Add(new Booking(eventId));
        return Task.CompletedTask;
    }

    public Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        var  booking = Bookings.FirstOrDefault(x => x.Id == bookingId);
        return Task.FromResult(booking);
    }
}