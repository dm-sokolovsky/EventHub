namespace BookingsService.Domain.Exceptions;

public sealed class NotFoundException(string message) : BookingsDomainException(message);
