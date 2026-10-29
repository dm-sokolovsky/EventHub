using EventHab.Application.Contracts;
using EventHab.Application.Repositories;
using EventHab.Application.Services.Abstractions;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Models;

namespace EventHab.Application.Services;

public sealed class BookingService(
    IBookingRepository bookingRepository,
    IEventRepository eventRepository,
    ICurrentUser currentUser
        ) : IBookingService
{

    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly IBookingRepository _bookingRepository = bookingRepository;
    private readonly IEventRepository _eventRepository = eventRepository;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<BookingInfo> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        await BookingLock.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.Now;
            var activeCount = 1;
            var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
                         ?? throw new NotFoundException("Event not found");

            if (!@event.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");
            
            if (@event.IsActive(now))
                throw new EventAlreadyStartedException(eventId, now);

            var booking = Booking.CreatePending(eventId, userId, activeCount);
            await _bookingRepository.AddAsync(booking, cancellationToken);
            
            return ToInfo(booking);
        }
        finally
        {
            BookingLock.Release();
        }
    }

    public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
                      ?? throw new NotFoundException("Booking not found");

        return ToInfo(booking);
    }

    public async Task<BookingInfo> CancelledBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
                      ?? throw new NotFoundException("Booking not found");
        
        if (booking.UserId != _currentUser.Id && !_currentUser.IsAdmin)
            throw new BookingAccessDeniedException(bookingId, _currentUser.Id);
        
        booking.Cancel();
        
        await _bookingRepository.SaveChangesAsync(cancellationToken);
        
        return ToInfo(booking);
    }


    private static BookingInfo ToInfo(Booking booking) => new()
    {
        Id = booking.Id,
        EventId = booking.EventId,
        Status = booking.Status,
        CreatedAt = booking.CreatedAt,
        ProcessedAt = booking.ProcessedAt
    };
}