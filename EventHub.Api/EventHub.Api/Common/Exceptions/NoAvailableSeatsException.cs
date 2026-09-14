using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Common.Exceptions;

public class NoAvailableSeatsException : Exception
{
    public NoAvailableSeatsException(string message) : base(message) { }
    
    internal ProblemDetails ToProblemDetails()
        => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "",
            Detail = Message,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5"
        };
}