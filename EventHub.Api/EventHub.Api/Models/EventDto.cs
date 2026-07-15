namespace EventHub.Api;

public record EventDto
(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt
);

public record EventCreatedDto
(
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt
);