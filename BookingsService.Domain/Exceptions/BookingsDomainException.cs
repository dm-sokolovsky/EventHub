namespace BookingsService.Domain.Exceptions;

public class BookingsDomainException : Exception
{
    public BookingsDomainException(string message) : base(message) { }
}
