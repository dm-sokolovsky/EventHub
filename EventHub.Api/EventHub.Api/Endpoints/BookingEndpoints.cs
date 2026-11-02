using System.Net;
using EventHab.Application.Contracts;
using EventHab.Application.Services.Abstractions;
using EventHub.Api.Common;
using EventHub.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Endpoints;

internal static class BookingEndpoints
{
    internal static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapPost("/events/{eventId:guid}/book", async (
                Guid eventId,
                IBookingService bookingService,
                HttpContext httpContext,
                ICurrentUser currentUser,
                CancellationToken cancellationToken) =>
            {
                var booking = await bookingService.CreateBookingAsync(eventId, currentUser.Id, cancellationToken);

                var location = $"/api/bookings/{booking.Id}";
                httpContext.Response.Headers.Location = location;

                return Results.Accepted(location, booking);
            })
            .WithName("CreateBooking")
            .Produces<BookingInfo>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        group.MapGet("/bookings/{id:guid}", async (
                Guid id,
                IBookingService bookingService,
                CancellationToken cancellationToken) =>
            {
                var booking = await bookingService.GetBookingByIdAsync(id, cancellationToken);
                return Results.Ok(booking);
            })
            .WithName("GetBookingById")
            .Produces<BookingInfo>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        group.MapDelete("/bookings/{id:guid}/cancel", async (
                Guid id,
                IBookingService bookingService,
                CancellationToken cancellationToken
        ) =>
        {
            var booking = await bookingService.CancelledBookingByIdAsync(id, cancellationToken);
            return Results.Ok(booking);
        })
        .WithName("CancelBooking")
        .Produces<BookingInfo>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .RequireAuthorization();

        return app;
    }
}