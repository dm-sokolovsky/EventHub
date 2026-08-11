using EventHub.Api.Models.Booking;

namespace EventHub.Api.Services;

public class BookingService : IBookingService
{

    private static List<Booking> Bookings { get; } = [];

    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        var booking = new Booking(eventId);
        Bookings.Add(booking);
        return Task.FromResult(booking);
    }

    public Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        var  booking = Bookings.FirstOrDefault(x => x.Id == bookingId);
        return Task.FromResult(booking);
    }
}