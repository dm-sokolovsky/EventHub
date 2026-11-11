namespace EventHab.Application.Services.Abstractions;

public interface ICurrentUser
{
    Guid Id { get; }
    bool IsAdmin { get; }
}