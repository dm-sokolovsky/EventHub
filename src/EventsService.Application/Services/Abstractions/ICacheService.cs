namespace EventsService.Application.Services.Abstractions;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task<bool> SetAsync<T>(string key, T value, TimeSpan expiration);
    Task RemoveAsync(string key);
}
