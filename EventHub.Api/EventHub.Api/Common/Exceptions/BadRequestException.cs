namespace EventHub.Api.Common.Exceptions;

public class BadRequestException : ApiException
{
    public BadRequestException(string message) : base(message, StatusCodes.Status400BadRequest) { }
}