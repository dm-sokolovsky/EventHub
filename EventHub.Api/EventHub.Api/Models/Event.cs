using System.ComponentModel.DataAnnotations;

namespace EventHub.Api.Models;

public class Event
{
    public Guid Id { get; set; }
    
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    
    public DateTime StartAt { get; set; }
    
    public DateTime EndAt { get; set; }

    public Event(string title, string? description, DateTime startAt, DateTime endAt)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
    }
}