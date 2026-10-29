using EventHub.Domain.Exceptions;

namespace EventHub.Domain.Models;

/// <summary>
/// Пользователь системы
/// </summary>
public sealed class User
{
    /// <summary>
    /// Уникальный идентификатор пользователя
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Логин пользователя
    /// </summary>
    public string Login { get; private set; } = null!;

    /// <summary>
    /// Хеш пароля; сам пароль в системе не хранится
    /// </summary>
    public string PasswordHash { get; private set; } = null!;

    /// <summary>
    /// Роль пользователя
    /// </summary>
    public Role Role { get; private set; }

    private readonly List<Booking> _bookings = [];
    
    public IReadOnlyCollection<Booking> Bookings => _bookings;

    private User() {}

    /// <summary>
    /// Создание пользователя
    /// </summary>
    /// <param name="id"></param>
    /// <param name="login"></param>
    /// <param name="passwordHash"></param>
    /// <param name="role"></param>
    private User(
        Guid id,
        string login,
        string passwordHash,
        Role role)
    {
        Id = id;
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }

    public static User Create(
        string? login,
        string? passwordHash,
        Role role = Role.User)
    {
        ThrowIfNotValid(login, passwordHash, role);

        return new User(Guid.NewGuid(), login!.Trim(), passwordHash!, role);
    }

    public void ChangePasswordHash(string? passwordHash)
    {
        ThrowIfNotValid(Login, passwordHash, Role);

        PasswordHash = passwordHash!;
    }

    public void ChangeRole(Role role)
    {
        ThrowIfNotValid(Login, PasswordHash, role);

        Role = role;
    }

    private static void ThrowIfNotValid(
        string? login,
        string? passwordHash,
        Role role)
    {
        var errors = new Dictionary<string, ICollection<string>>();

        if (string.IsNullOrWhiteSpace(login))
            AddError(errors, nameof(Login), "Login cannot be empty");

        if (string.IsNullOrWhiteSpace(passwordHash))
            AddError(errors, nameof(PasswordHash), "PasswordHash cannot be empty");

        if (!Enum.IsDefined(role))
            AddError(errors, nameof(Role), "Role is not defined");

        if (errors.Any())
            throw new ValidationException(errors);
    }

    private static void AddError(Dictionary<string, ICollection<string>> errors, string field, string message)
    {
        if (!errors.ContainsKey(field))
            errors[field] = new List<string>();

        errors[field].Add(message);
    }
}
