namespace EventsService.Domain.Exceptions;

public sealed class NotFoundException(string message) : EventsDomainException(message);
