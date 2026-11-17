using EventHub.Contracts;

namespace EventsService.Application;

public interface IEventPublisher
{
    Task PublishAsync(EventChanged message, CancellationToken cancellationToken = default);
}
