namespace EventsService.Application.Services.Abstractions;

public sealed class CacheOptions
{
    public const string SectionName = "Redis";

    public TimeSpan EventTtl { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan TopEventsTtl { get; set; } = TimeSpan.FromMinutes(5);
}
