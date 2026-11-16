using BookingsService.Application.Services.Abstractions;

namespace BookingsService.Tests;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsAdmin { get; set; }
}
