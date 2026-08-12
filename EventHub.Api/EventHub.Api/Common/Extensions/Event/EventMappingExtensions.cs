using EventHub.Api.Contracts;
using EventHub.Api.Models;
using EventHub.Api.Models.Event;

namespace EventHub.Api.Extensions.Event;

public static class EventMappingExtensions
{
    // StartAt/EndAt are DateTime? only so [Required] can catch a missing field (see EventDto.cs);
    // [ApiController] rejects the request with 400 before the action runs if either is null,
    // so by the time we map to the domain Event they're guaranteed present.
    public static Models.Event.Event ToEvent(this EventUpsertDto dto) =>
        new(dto.Title, dto.Description, dto.StartAt!.Value, dto.EndAt!.Value);

    public static EventDto ToDto(this Models.Event.Event @event) =>
        new(@event.Id, @event.Title, @event.Description, @event.StartAt, @event.EndAt);

    public static EventFilter ToEventFilter(this EventFilterDto dto) =>
        new(dto.Title, dto.From, dto.To);
}
