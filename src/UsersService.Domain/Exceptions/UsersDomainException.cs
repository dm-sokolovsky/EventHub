namespace UsersService.Domain.Exceptions;

public class UsersDomainException : Exception
{
    public UsersDomainException(string message) : base(message) { }
}
