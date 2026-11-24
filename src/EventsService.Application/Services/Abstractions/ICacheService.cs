using EventsService.Application.Contracts;

namespace EventsService.Application.Services.Abstractions;

public interface ICacheService
{
    Task<EventInfo?> GetByKeyAsync(string key);
    Task<bool> SetAsync(string key, EventInfo value, TimeSpan expiration);
    Task RemoveAsync(string key);
}
