namespace EventHub.Domain.Exceptions;

public sealed class ValidationException : Exception
{
    public IDictionary<string, ICollection<string>> Errors { get; } = new Dictionary<string, ICollection<string>>();

    public ValidationException(IDictionary<string, ICollection<string>> errors) : base("Validation failed")
    {
        Errors = errors;
    }

    public ValidationException(string field, string error) : base("Validation failed")
    {
        Errors = new Dictionary<string, ICollection<string>>
        {
            { field, new[] { error } }
        };
    }
}