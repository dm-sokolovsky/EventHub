namespace EventHub.Domain.Exceptions;

public sealed class LoginAlreadyTakenException(string login) 
    : DomainException($"The login '{login}' is already taken")
{
    
}