namespace EventsService.Domain.Exceptions;

public class EventsDomainException : Exception
{
    public EventsDomainException(string message) : base(message) { }
}
