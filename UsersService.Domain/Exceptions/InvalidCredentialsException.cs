namespace UsersService.Domain.Exceptions;

public sealed class InvalidCredentialsException()
    : UsersDomainException("Incorrect login or password");
