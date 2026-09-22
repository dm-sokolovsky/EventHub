using EventHub.Api.Common.Exceptions;
using EventHub.Api.Contracts;
using EventHub.Api.DataAccess;
using EventHub.Api.DataAccess.Repositories;
using EventHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.Services;

public sealed class BookingService(
    BookingRepository bookingRepository,
    EventRepository eventRepository
        ) : IBookingService
{

    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly BookingRepository _bookingRepository = bookingRepository;
    private readonly EventRepository _eventRepository = eventRepository;

    public async Task<BookingInfo> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await BookingLock.WaitAsync(cancellationToken);
        try
        {
            var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
                         ?? throw new NotFoundException("Event not found");

            if (!@event.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");

            var booking = Booking.CreatePending(eventId);
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
    
    private static BookingInfo ToInfo(Booking booking) => new()
    {
        Id = booking.Id,
        EventId = booking.EventId,
        Status = booking.Status,
        CreatedAt = booking.CreatedAt,
        ProcessedAt = booking.ProcessedAt
    };
}