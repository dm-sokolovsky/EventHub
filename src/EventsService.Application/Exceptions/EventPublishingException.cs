namespace EventsService.Application.Exceptions;

/// <summary>
/// Ошибка доставки события в брокер — временная, повторить позже
/// </summary>
public sealed class EventPublishingException : Exception
{
    public EventPublishingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
