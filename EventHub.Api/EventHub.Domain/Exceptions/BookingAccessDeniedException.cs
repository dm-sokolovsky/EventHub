namespace EventHub.Domain.Exceptions;

public class BookingAccessDeniedException(Guid bookingId, Guid userId) 
    : DomainException ($"The user with {userId} cannot cancel the booking with {bookingId}");