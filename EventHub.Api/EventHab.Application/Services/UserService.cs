using EventHab.Application.Contracts;
using EventHab.Application.Repositories;
using EventHab.Application.Services.Abstractions;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Models;

namespace EventHab.Application.Services;

public sealed class UserService(
    IUserRepository userRepository,
    IPasswordHasher hasher,
    IJwtTokenService tokens
    ) : IUserService
{
    public async Task<Guid> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var login = request.Login.Trim();

        if (await userRepository.ExistsByLoginAsync(login, ct))
            throw new LoginAlreadyTakenException(login);

        var user = User.Create(login, hasher.Hash(request.Password));
        await userRepository.AddAsync(user, ct);
        await userRepository.SaveChangesAsync(ct);

        return user.Id;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await userRepository.GetByLoginAsync(request.Login.Trim(), ct);

        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return new AuthResult(tokens.GenerateToken(user.Id, user.Login, user.Role.ToString()));
    }
}