using EventsService.Domain.Models;

namespace EventsService.Application.Services.Abstractions;

public interface ICacheService
{
    Task<Event?> GetByKeyAsync(string id);
    Task<bool> SetAsync(Event value, TimeSpan expiration);
    Task RemoveAsync(string id); 
}