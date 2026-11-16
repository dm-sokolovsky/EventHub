using UsersService.Application.Contracts;
using UsersService.Application.Repositories;
using UsersService.Application.Services.Abstractions;
using UsersService.Domain.Exceptions;
using UsersService.Domain.Models;

namespace UsersService.Application.Services;

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
        
        if (user is null || !hasher.Verify(user.PasswordHash, request.Password))
            throw new InvalidCredentialsException();

        return new AuthResult(tokens.GenerateToken(user.Id, user.Login, user.Role.ToString()));
    }
}