namespace EventHub.Domain.Exceptions;

public sealed class InvalidCredentialsException() 
    : DomainException("Incorrect login or password")
{
    
}