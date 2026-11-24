using System.Text.Json;
using EventsService.Application.Services.Abstractions;
using EventsService.Domain.Models;
using StackExchange.Redis;

namespace EventsService.Infrastructure.Redis;

public sealed class CacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IDatabase _redis = redis.GetDatabase();
    
    public async Task<Event?> GetByKeyAsync(string key)
    {
        var cached = await _redis.StringGetAsync(key);
        
        if (!cached.HasValue)
            return null;
        
        return JsonSerializer.Deserialize<Event>(cached.ToString());
    }

    public async Task<bool> SetAsync(Event value,  TimeSpan expiration)
    {
        string key = $"event:{value.Id}";
        var json = JsonSerializer.Serialize(value);
        
        return await _redis.StringSetAsync(key, json, expiration);
    }

    public async Task RemoveAsync(string key)
    {
        await _redis.KeyDeleteAsync(key);
    }
}