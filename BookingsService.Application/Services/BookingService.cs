using BookingsService.Application.Contracts;
using BookingsService.Application.Repositories;
using BookingsService.Application.Services.Abstractions;
using BookingsService.Domain.Exceptions;
using BookingsService.Domain.Models;

namespace BookingsService.Application.Services;

public sealed class BookingService(
    IBookingRepository bookingRepository,
    IEventsGateway eventsGateway,
    ICurrentUser currentUser
        ) : IBookingService
{

    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    public async Task<BookingInfo> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        await BookingLock.WaitAsync(cancellationToken);

        try
        {
            var now = DateTime.UtcNow;

            // Получаем событие
            var eventSnapshot = await eventsGateway
                                    .GetAsync(eventId, cancellationToken)
                                ?? throw new NotFoundException("Event not found");

            // Проверяем дату события
            if (eventSnapshot.StartAt <= now)
                throw new EventAlreadyStartedException(eventId, now);

            // Подсчитываем активные брони пользователя
            var activeCount = await bookingRepository
                .CountActiveBookingsAsync(userId, cancellationToken);

            // Проверяем лимит при создании брони
            var booking = Booking.CreatePending(
                eventId,
                userId,
                activeCount);

            // Резервируем место только после успешных проверок
            if (!await eventsGateway.TryReserveSeatsAsync(eventId, cancellationToken))
                throw new NoAvailableSeatsException(
                    "No available seats for this event");

            await bookingRepository.AddAsync(
                booking,
                cancellationToken);

            return ToInfo(booking);
        }
        finally
        {
            BookingLock.Release();
        }
    }

    public async Task<BookingInfo> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken)
                      ?? throw new NotFoundException("Booking not found");

        return ToInfo(booking);
    }

    public async Task<BookingInfo> CancelledBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken)
                      ?? throw new NotFoundException("Booking not found");

        if (booking.UserId != currentUser.Id && !currentUser.IsAdmin)
            throw new BookingAccessDeniedException(bookingId, currentUser.Id);

        booking.Cancel();

        await bookingRepository.SaveChangesAsync(cancellationToken);

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
