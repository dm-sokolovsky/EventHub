namespace EventHub.Api.Common.Exceptions;

public class NotFoundException : ApiException
{
    public NotFoundException(string message) : base(message, StatusCodes.Status404NotFound) { }
}