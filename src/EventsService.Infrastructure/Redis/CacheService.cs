using System.Text.Json;
using EventsService.Application.Services.Abstractions;
using EventsService.Domain.Models;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EventsService.Infrastructure.Redis;

public sealed class CacheService(IConnectionMultiplexer redis, ILogger<CacheService> logger) : ICacheService
{
    private readonly IDatabase _redis = redis.GetDatabase();
    private readonly ILogger<CacheService> _logger = logger;
    
    public async Task<Event?> GetByKeyAsync(string key)
    {

        try
        {
            var cached = await _redis.StringGetAsync(key);

            if (cached.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<Event>(cached.ToString()) ?? null;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis is connected, read key {key} was skipped", key);
            return null;
        }
    }

    public async Task<bool> SetAsync(string key, Event value,  TimeSpan expiration)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);

            return await _redis.StringSetAsync(key, json, expiration);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis is connected, write key {key} not cached", key);
        }
        return false;
    }

    public async Task RemoveAsync(string key)
    {
        await _redis.KeyDeleteAsync(key);
    }
}