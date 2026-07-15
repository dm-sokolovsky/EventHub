namespace EventHub.Api.Models;

public record Event(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt
);