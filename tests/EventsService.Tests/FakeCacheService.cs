using EventsService.Application.Contracts;
using EventsService.Application.Services.Abstractions;

namespace EventsService.Tests;

public sealed class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, EventInfo> _items = [];

    public Task<EventInfo?> GetByKeyAsync(string key) =>
        Task.FromResult(_items.GetValueOrDefault(key));

    public Task<bool> SetAsync(string key, EventInfo value, TimeSpan expiration)
    {
        _items[key] = value;
        return Task.FromResult(true);
    }

    public Task RemoveAsync(string key)
    {
        _items.Remove(key);
        return Task.CompletedTask;
    }
}
