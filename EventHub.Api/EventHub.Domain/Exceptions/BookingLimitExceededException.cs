namespace EventHub.Domain.Exceptions;

public sealed class BookingLimitExceededException(Guid userId, int limit)
    : DomainException($"The user with {{userId}} already has {{limit}} active bookings, which is the maximum")
{
    public Guid UserId { get; } = userId;
    public int Limit { get; } = limit;
}