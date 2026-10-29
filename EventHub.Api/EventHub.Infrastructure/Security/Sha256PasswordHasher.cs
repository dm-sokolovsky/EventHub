using System.Security.Cryptography;
using System.Text;
using EventHab.Application.Services.Abstractions;

namespace EventHub.Infrastructure;

public class Sha256PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        
        return Convert.ToHexString(bytes);
    }

    public bool Verify(string hash, string password)
    {
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        byte[] result;

        try
        {
            result = Convert.FromHexString(hash);
        }
        catch (FormatException)
        {
            return false;
        }
        
        return CryptographicOperations.FixedTimeEquals(actual, result);
    }
}