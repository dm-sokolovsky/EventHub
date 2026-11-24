using EventsService.Application.Services.Abstractions;

namespace EventsService.Tests;

public sealed class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, object?> _items = [];

    public Task<T?> GetAsync<T>(string key) =>
        Task.FromResult(_items.TryGetValue(key, out var value) ? (T?)value : default);

    public Task<bool> SetAsync<T>(string key, T value, TimeSpan expiration)
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
