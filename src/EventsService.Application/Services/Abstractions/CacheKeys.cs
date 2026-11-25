namespace EventsService.Application.Services.Abstractions;

public static class CacheKeys
{
    public static string Event(Guid id) =>  $"event:{id}";
    public static string Top10 => "event:top10";
}