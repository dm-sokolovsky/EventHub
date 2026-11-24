using EventsService.Domain.Models;

namespace EventsService.Application.Services.Abstractions;

public interface ICacheService
{
    Task<Event?> GetByKeyAsync(string key);
    Task<bool> SetAsync(string key, Event value, TimeSpan expiration);
    Task RemoveAsync(string key); 
}