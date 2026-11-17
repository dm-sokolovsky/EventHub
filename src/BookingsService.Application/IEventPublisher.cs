using EventHub.Contracts;

namespace BookingsService.Application;

public interface IEventPublisher
{
    Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken = default);
}