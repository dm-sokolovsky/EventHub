namespace BookingsService.Domain.Exceptions;

public sealed class EventAlreadyStartedException(Guid eventId, DateTime startAt)
    : BookingsDomainException($"The event {eventId} has already started at {startAt}, the booking is closed")
{
    public Guid EventId { get; } = eventId;
}
