using System.Net;
using EventHub.Api.Common.Exceptions;
using EventHub.Api.Models;
using ValidationException = EventHub.Api.Common.Exceptions.ValidationException;

namespace EventHub.Api.Common;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger
        )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            if (httpContext.Response.HasStarted)
            {
                _logger.LogError(
                    ex,
                    "Exception after response started. Method={Method}, Path={Path}, RequestId={RequestId}",
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    httpContext.Request.Headers["x-request-id"]);
                return;
            }

            var statusCode = MapStatusCode(ex);

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(
                    ex,
                    "Unhandled exception. Method={Method}, Path={Path}, RequestId={RequestId}",
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    httpContext.Request.Headers["x-request-id"]);
            }
            else
            {
                _logger.LogWarning(
                    "Request failed with {StatusCode}: {Message}. Method={Method}, Path={Path}, RequestId={RequestId}",
                    statusCode,
                    ex.Message,
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    httpContext.Request.Headers["x-request-id"]);
            }

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";

            var error = new ApiBaseResult
            {
                Success = false,
                StatusCode = (HttpStatusCode)statusCode,
                Message = ex.Message
            };

            await httpContext.Response.WriteAsJsonAsync(error);
        }
    }
    
    private static int MapStatusCode(Exception ex)
        => ex switch
        {
            ValidationException ve => StatusCodes.Status400BadRequest,
            NotFoundException nfe => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };
}