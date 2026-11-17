using EventHub.Contracts;
using EventsService.Application;

namespace EventsService.Tests;

/// <summary>
/// Фиктивный издатель: публикует «в никуда», запоминая сообщения для проверок
/// </summary>
public sealed class FakeEventPublisher : IEventPublisher
{
    public List<EventChanged> Published { get; } = [];

    public Task PublishAsync(EventChanged message, CancellationToken cancellationToken = default)
    {
        Published.Add(message);
        return Task.CompletedTask;
    }
}
