using EventsService.Infrastructure.Redis;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace EventsService.Tests;

public sealed class CacheServiceTests : IDisposable
{
    // Порт 6390 заведомо закрыт: клиент с abortConnect=false стартует без Redis, CacheService обязан деградировать, а не бросать.
    private readonly ConnectionMultiplexer _redis = ConnectionMultiplexer.Connect(
        "localhost:6390,abortConnect=false,connectTimeout=500,syncTimeout=500,asyncTimeout=500");

    private readonly CacheService _sut;

    public CacheServiceTests()
    {
        _sut = new CacheService(_redis, NullLogger<CacheService>.Instance);
    }

    [Fact]
    public async Task GetAsync_RedisUnavailable_ReturnsDefaultInsteadOfThrowing()
    {
        var result = await _sut.GetAsync<string>("event:1");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_RedisUnavailable_ReturnsFalseInsteadOfThrowing()
    {
        var stored = await _sut.SetAsync("event:1", "value", TimeSpan.FromMinutes(5));

        Assert.False(stored);
    }

    [Fact]
    public async Task RemoveAsync_RedisUnavailable_DoesNotThrow()
    {
        await _sut.RemoveAsync("event:1");
    }

    public void Dispose() => _redis.Dispose();
}
