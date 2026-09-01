using EventHub.Api.Common.Exceptions;
using EventHub.Api.Models.Booking;

namespace EventHub.Api.Services;

public class BookingService(IEventService eventService) : IBookingService
{

    private static List<Booking> Bookings { get; } = [];

    // Booking теперь читается и изменяется не только из запросов контроллера,
    // но и из BookingProcessingBackgroundService на отдельном потоке —
    // List<T> не потокобезопасен, поэтому все обращения к Bookings идут под lock.
    private readonly object _bookingLock = new();

    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        
        lock (_bookingLock)
        {
            var @event = eventService.GetEventById(eventId)
                         ?? throw new NotFoundException($"Не удалось найти событие по {eventId}");
            
            var isReserveSeats = @event.TryReserveSeats();

            if (!isReserveSeats)
                throw new NoAvailableSeatsException("No available seats for this event");

            var booking = new Booking(@event.Id);
            
            Bookings.Add(booking);
                
            return Task.FromResult(booking);
        }
    }

    public Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        lock (_bookingLock)
        {
            var booking = Bookings.FirstOrDefault(x => x.Id == bookingId);
            return Task.FromResult(booking);
        }
    }

    public Task<IReadOnlyList<Booking>> GetPendingBookingsAsync()
    {
        lock (_bookingLock)
        {
            IReadOnlyList<Booking> pending = Bookings
                .Where(x => x.Status == BookingStatus.Pending)
                .ToList();

            return Task.FromResult(pending);
        }
    }

    public Task UpdateBookingAsync(Booking booking)
    {
        lock (_bookingLock)
        {
            var index = Bookings.FindIndex(x => x.Id == booking.Id);

            if (index != -1)
                Bookings[index] = booking;
        }

        return Task.CompletedTask;
    }
}