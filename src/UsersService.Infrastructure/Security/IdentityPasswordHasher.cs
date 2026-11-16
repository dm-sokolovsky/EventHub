using UsersService.Application.Services.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace UsersService.Infrastructure.Security;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password)
    {
        return _hasher.HashPassword(null!, password);
    }

    public bool Verify(string hash, string password)
    {
        var result = _hasher.VerifyHashedPassword(
            null!,
            hash,
            password);

        return result != PasswordVerificationResult.Failed;
    }
}