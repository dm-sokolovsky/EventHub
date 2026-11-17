using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using EventHub.Contracts;
using UsersService.Application.Repositories;
using UsersService.Application.Services;
using UsersService.Application.Services.Abstractions;
using UsersService.Domain.Exceptions;
using UsersService.Domain.Models;
using UsersService.Infrastructure;
using UsersService.Infrastructure.Options;
using UsersService.Infrastructure.Repositories;
using UsersService.Infrastructure.Security;

namespace UsersService.Tests;

public sealed class UserServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IUserService _userService;

    public UserServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton(Options.Create(new JwtOptions
        {
            Secret = "Test-secret-key-with-at-least-32-bytes!",
            Issuer = "Tests",
            Audience = "Tests"
        }));
        services.AddScoped<IUserService, UserService>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _userService = _scope.ServiceProvider.GetRequiredService<IUserService>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    #region RegisterAsync

    [Fact]
    public async Task RegisterAsync_WithNewLogin_ReturnsUserId()
    {
        var id = await _userService.RegisterAsync(
            new RegisterRequest("new-user", "Password123!"),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateLogin_ThrowsLoginAlreadyTakenException()
    {
        await _userService.RegisterAsync(
            new RegisterRequest("duplicate", "Password123!"),
            CancellationToken.None);

        await Assert.ThrowsAsync<LoginAlreadyTakenException>(() =>
            _userService.RegisterAsync(
                new RegisterRequest("duplicate", "OtherPassword1!"),
                CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_PersistsUserWithUserRole()
    {
        var repository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var id = await _userService.RegisterAsync(
            new RegisterRequest("role-check", "Password123!"),
            CancellationToken.None);

        var user = await repository.GetByLoginAsync("role-check", CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal(id, user.Id);
        Assert.Equal(Role.User, user.Role);
        Assert.NotEqual("Password123!", user.PasswordHash);
    }

    #endregion

    #region LoginAsync

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        await _userService.RegisterAsync(
            new RegisterRequest("login-ok", "Password123!"),
            CancellationToken.None);

        var result = await _userService.LoginAsync(
            new LoginRequest("login-ok", "Password123!"),
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
    }

    [Fact]
    public async Task LoginAsync_WithUnknownLogin_ThrowsInvalidCredentialsException()
    {
        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _userService.LoginAsync(
                new LoginRequest("no-such-user", "Password123!"),
                CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentialsException()
    {
        await _userService.RegisterAsync(
            new RegisterRequest("wrong-pass", "Password123!"),
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _userService.LoginAsync(
                new LoginRequest("wrong-pass", "NotThePassword1!"),
                CancellationToken.None));
    }

    #endregion
}
