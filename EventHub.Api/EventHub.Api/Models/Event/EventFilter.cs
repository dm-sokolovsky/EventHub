namespace EventHub.Api.Models;

public class EventFilter
{
    public string? Title {get; private set;}
    public DateTime? From {get; private set;}
    public DateTime? To {get; private set;}

    public EventFilter(string? title, DateTime? from, DateTime? to)
    {
        Title = title;
        From = from;
        To = to;
    }
}