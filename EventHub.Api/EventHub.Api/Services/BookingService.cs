using EventHub.Api.Models.Booking;

namespace EventHub.Api.Services;

public class BookingService : IBookingService
{

    private static List<Booking> Bookings { get; } = [];

    // Booking теперь читается и изменяется не только из запросов контроллера,
    // но и из BookingProcessingBackgroundService на отдельном потоке —
    // List<T> не потокобезопасен, поэтому все обращения к Bookings идут под lock.
    private readonly object _syncRoot = new();

    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        var booking = new Booking(eventId);

        lock (_syncRoot)
        {
            Bookings.Add(booking);
        }

        return Task.FromResult(booking);
    }

    public Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        lock (_syncRoot)
        {
            var booking = Bookings.FirstOrDefault(x => x.Id == bookingId);
            return Task.FromResult(booking);
        }
    }

    public Task<IReadOnlyList<Booking>> GetPendingBookingsAsync()
    {
        lock (_syncRoot)
        {
            IReadOnlyList<Booking> pending = Bookings
                .Where(x => x.Status == BookingStatus.Pending)
                .ToList();

            return Task.FromResult(pending);
        }
    }

    public Task UpdateBookingAsync(Booking booking)
    {
        lock (_syncRoot)
        {
            var index = Bookings.FindIndex(x => x.Id == booking.Id);

            if (index != -1)
                Bookings[index] = booking;
        }

        return Task.CompletedTask;
    }
}