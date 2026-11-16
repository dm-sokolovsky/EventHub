namespace BookingsService.Domain.Exceptions;

public sealed class BookingAccessDeniedException(Guid bookingId, Guid userId)
    : BookingsDomainException($"The user with {userId} cannot cancel the booking with {bookingId}");
