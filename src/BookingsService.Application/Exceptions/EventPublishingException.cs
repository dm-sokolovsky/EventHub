namespace BookingsService.Application.Exceptions;

public sealed class EventPublishingException : Exception
{
    public EventPublishingException(string message, Exception innerException) : base(message, innerException)
    {}
}