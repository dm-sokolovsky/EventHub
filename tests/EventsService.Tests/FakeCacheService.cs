using EventsService.Application.Services.Abstractions;

namespace EventsService.Tests;

public sealed class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, object?> _items = [];
    private readonly Dictionary<string, TimeSpan> _expirations = [];

    public IReadOnlyDictionary<string, TimeSpan> Expirations => _expirations;

    public Task<T?> GetAsync<T>(string key) =>
        Task.FromResult(_items.TryGetValue(key, out var value) ? (T?)value : default);

    public Task<bool> SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        _items[key] = value;
        _expirations[key] = expiration;
        return Task.FromResult(true);
    }

    public Task RemoveAsync(string key)
    {
        _items.Remove(key);
        return Task.CompletedTask;
    }
}
