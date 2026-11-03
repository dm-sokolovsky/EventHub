namespace EventHub.Domain.Exceptions;

public class AccessDeniedException : Exception
{
    public AccessDeniedException(string message) : base(message) { }
}