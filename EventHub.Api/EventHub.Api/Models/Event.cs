using System.ComponentModel.DataAnnotations;

namespace EventHub.Api.Models;

public class Event : IValidatableObject
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; } 
    [Required(ErrorMessage = "Start At is required")]
    public DateTime StartAt { get; set; }
    [Required(ErrorMessage = "End At is required")]
    public DateTime EndAt { get; set; }
    
    public Event(string title, string? description, DateTime startAt, DateTime endAt)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndAt <= StartAt)
            yield return new ValidationResult("EndAt должен быть позже StartAt", [nameof(EndAt), nameof(StartAt)]);
    }
}