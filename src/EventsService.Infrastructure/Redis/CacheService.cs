using System.Text.Json;
using EventsService.Application.Services.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EventsService.Infrastructure.Redis;

public sealed class CacheService(IConnectionMultiplexer redis, ILogger<CacheService> logger) : ICacheService
{
    private readonly IDatabase _redis = redis.GetDatabase();
    private readonly ILogger<CacheService> _logger = logger;

    public async Task<T?> GetAsync<T>(string key)
    {
        if (!redis.IsConnected)
        {
            _logger.LogWarning("Redis is unavailable, read key {key} was skipped", key);
            return default;
        }

        try
        {
            var cached = await _redis.StringGetAsync(key);

            if (cached.IsNullOrEmpty)
                return default;

            return JsonSerializer.Deserialize<T>(cached.ToString());
        }
        catch (Exception ex) when(ex is RedisException or RedisTimeoutException)
        {
            _logger.LogWarning(ex, "Redis is unavailable, read key {key} was skipped", key);
            return default;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        if (!redis.IsConnected)
        {
            _logger.LogWarning("Redis is unavailable, key {key} was not cached", key);
            return false;
        }

        try
        {
            var json = JsonSerializer.Serialize(value);

            return await _redis.StringSetAsync(key, json, expiration);
        }
        catch (Exception ex) when(ex is RedisException or RedisTimeoutException)
        {
            _logger.LogWarning(ex, "Redis is unavailable, key {key} was not cached", key);
            return false;
        }
    }

    public async Task RemoveAsync(string key)
    {
        if (!redis.IsConnected)
        {
            _logger.LogWarning("Redis is unavailable, key {key} was not invalidated", key);
            return;
        }

        try
        {
            await _redis.KeyDeleteAsync(key);
        }
        catch (Exception ex) when(ex is RedisException or RedisTimeoutException)
        {
            _logger.LogWarning(ex, "Redis is unavailable, key {key} was not invalidated", key);
        }
    }
}
