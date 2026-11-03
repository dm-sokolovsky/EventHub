using EventHab.Application.Contracts;

namespace EventHab.Application.Services.Abstractions;

public interface IUserService
{
    Task<Guid> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct);
}