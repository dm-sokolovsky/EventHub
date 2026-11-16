namespace UsersService.Domain.Exceptions;

public sealed class LoginAlreadyTakenException(string login) 
    : UsersDomainException($"The login '{login}' is already taken")
{
    
}