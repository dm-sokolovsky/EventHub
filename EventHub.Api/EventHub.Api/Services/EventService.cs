using EventHub.Api.Models;
using EventHub.Api.Extensions;
using EventHub.Api.Extensions.Event;
using EventHub.Api.Models.Event;

namespace EventHub.Api.Services;

public class EventService : IEventService
{
    
    // Коллекция для манипуляции над событиями
    // TODO: static-состояние расшарено между всеми экземплярами EventService в рамках процесса,
    // включая параллельные тесты (EventHub.Tests, EventHub.IntegrationTests). Сейчас тесты
    // изолируются только за счёт Guid.NewGuid()-уникальных Title в фильтрах — это хрупко и не
    // защищает от коллизий, если тесты когда-нибудь начнут проверять totalCount/список без
    // фильтра. Нужен либо реальный сброс между тестами (метод EventService.Reset()/новый
    // инстанс-хранилище вместо static), либо явный DI-скоуп per-test/per-collection.
    private static List<Event> Events { get; } = [];

    public (List<Event> Items, int TotalCount) GetEvents(EventFilter eventFilter, int page, int pageSize)
    {
        var filtered = Events.AsQueryable()
            .TitleFilter(eventFilter.Title)
            .FromDateFilter(eventFilter.From)
            .ToDateFilter(eventFilter.To);

        var totalCount = filtered.Count();
        var items = filtered.Page(page, pageSize).ToList();

        return (items, totalCount);
    }



    public Event? GetEventById(Guid id) => Events.FirstOrDefault(x => x.Id == id);

    public Event CreateEvent(Event newEvent)
    {
        Events.Add(newEvent);
        return newEvent;
    }

    public Event? UpdateEvent(Guid id, Event updatedEvent)
    {
        var @event = Events.FirstOrDefault(x => x.Id == id);
        
        if (@event is null)
            return null;

        @event.UpdateDetails(
            updatedEvent.Title,
            updatedEvent.Description,
            updatedEvent.StartAt,
            updatedEvent.EndAt,
            updatedEvent.TotalSeats
            );
        
        return @event;
    }

    public bool DeleteEvent(Guid id)
    {
        var index = Events.FindIndex(x => x.Id == id);
        if (index == -1) return false;

        Events.RemoveAt(index);
        return true;
    }
}