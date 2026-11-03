namespace EventHab.Application.Contracts;

public sealed record RegisterRequest(string Login, string Password);

public sealed record LoginRequest(string Login, string Password);

public sealed record AuthResult(string AccessToken);