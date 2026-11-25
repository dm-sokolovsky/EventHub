using EventHub.Contracts;
using EventsService.Application.Repositories;
using EventsService.Application.Services.Abstractions;

namespace EventsService.Application.Services;

public sealed class BookingConfirmedHandler(
    IEventRepository eventRepository,
    ICacheService cacheService)
{
    public async Task<BookingConfirmedResult> HandleAsync(BookingConfirmed message, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetByIdAsync(message.EventId, cancellationToken);
        if (@event is null)
            return BookingConfirmedResult.EventNotFound;

        if (!@event.TryReserveSeats(message.Seats))
            return BookingConfirmedResult.SeatsUnavailable;

        await eventRepository.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(CacheKeys.Event(message.EventId));
        await cacheService.RemoveAsync(CacheKeys.Top10);

        return BookingConfirmedResult.Reserved;
    }
}
