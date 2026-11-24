using EventsService.Application.Contracts;
using EventsService.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace EventsService.Api.Endpoints;

internal static class EventEndpoints
{
    internal static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events");

        group.MapGet("/", async (
            IEventService eventService,
            [AsParameters] EventFilter eventFilter) =>
        {
            var events = await eventService.GetAllEventsAsync(eventFilter);
            return Results.Ok(events);
        })
        .WithName("GetAllEvents")
        .Produces<PaginatedResult<EventInfo>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id:guid}", async (Guid id, IEventService eventService) =>
        {
            var @event = await eventService.GetEventByIdAsync(id);
            return Results.Ok(@event);
        })
        .WithName("GetEventById")
        .Produces<EventInfo>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreateEvent request, IEventService eventService) =>
        {
            var createdEvent = await eventService.CreateEventAsync(request);
            return Results.Created($"/api/events/{createdEvent.Id}", createdEvent);
        })
        .WithName("CreateEvent")
        .Produces<EventInfo>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AdminOnly");

        group.MapPut("/{id:guid}", async (Guid id, EventUpsert request, IEventService eventService) =>
        {
            var updatedEvent = await eventService.UpdateEventAsync(id, request);
            return Results.Ok(updatedEvent);
        })
        .WithName("UpdateEvent")
        .Produces<EventInfo>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
        .RequireAuthorization("AdminOnly");

        group.MapDelete("/{id:guid}", async (Guid id, IEventService eventService) =>
        {
            await eventService.DeleteEventAsync(id);
            return Results.NoContent();
        })
        .WithName("DeleteEvent")
        .Produces<EventInfo>(StatusCodes.Status204NoContent)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .RequireAuthorization("AdminOnly");

        group.MapGet("/top", async (IEventService eventService) =>
            {
                var top = await eventService.GetTopEventsCachedAsync();
                
                return Results.Ok(top);
            })
            .WithName("GetTopEvents")
            .Produces<List<EventInfo>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return app;
    }
}
