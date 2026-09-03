using EventHub.Api.Models;
using EventHub.Api.Models.Event;
using EventHub.Api.Services;

namespace EventHub.Tests;

// TODO: EventService.Events — static (см. EventHub.Api/Services/EventService.cs), поэтому оно
// расшарено между ВСЕМИ EventService в процессе независимо от этой фикстуры. Events ниже —
// отдельная, никогда не связанная с EventService коллекция: она нигде не передаётся в
// EventService (сеттера/конструктора для этого нет), поэтому GetEventById/GetEvents её не видят.
// Поле сейчас фактически мёртвое — используется только как typed-заглушка в конструкторах
// тестовых классов (EventServiceTests._events и аналоги), которые сами его тоже не читают.
// Либо удалить Events отсюда как мёртвый код, либо (если цель — детерминированные seed-данные)
// реально прокидывать их в EventService после добавления туда способа сброса/сидирования.
public class EventServiceFixture
{
    public EventService EventService { get; set; }
    public IEnumerable<Event> Events { get; set; }

    public EventServiceFixture()
    {
        EventService = new EventService();
        Events = new List<Event>()
        {
            new Event("test1", "test1",  DateTime.UtcNow,  DateTime.UtcNow + TimeSpan.FromDays(1), 10),
            new Event("test2", "test2",  DateTime.UtcNow,  DateTime.UtcNow + TimeSpan.FromDays(2), 10),
            new Event("test3", "test3",  DateTime.UtcNow,  DateTime.UtcNow + TimeSpan.FromDays(3), 10),
            new Event("test4", "test4",  DateTime.UtcNow,  DateTime.UtcNow + TimeSpan.FromDays(4), 10)
        };
    }
}