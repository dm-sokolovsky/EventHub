namespace EventHub.Api.Common.Exceptions;

public class NoAvailableSeatsException : ApiException
{
    public NoAvailableSeatsException(string message) : base(message, StatusCodes.Status409Conflict) { }
}