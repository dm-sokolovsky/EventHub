using EventHub.Api.Contracts;
using EventHub.Api.Models;

namespace EventHub.Api.Extensions;

public static class EventMappingExtensions
{
    public static Event ToEvent(this EventUpsertDto dto) =>
        new(dto.Title, dto.Description, dto.StartAt, dto.EndAt);

    public static EventDto ToDto(this Event @event) =>
        new(@event.Id, @event.Title, @event.Description, @event.StartAt, @event.EndAt);

    public static EventFilter ToEventFilter(this EventFilterDto dto) =>
        new(dto.Title, dto.From, dto.To);
}
