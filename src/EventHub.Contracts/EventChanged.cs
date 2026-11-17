namespace EventHub.Contracts;

/// <summary>
/// Тип изменения события в топике events-changed
/// </summary>
public enum EventChangeType
{
    Created,
    Updated,
    Deleted
}

/// <summary>
/// Событие «событие изменено» — контракт сообщения топика events-changed.
/// Публикует EventsService; BookingsService поддерживает локальную копию данных.
/// </summary>
/// <param name="Id">Идентификатор события</param>
/// <param name="Title">Заголовок</param>
/// <param name="StartAt">Дата и время начала (UTC)</param>
/// <param name="EndAt">Дата и время окончания (UTC)</param>
/// <param name="TotalSeats">Общее количество мест</param>
/// <param name="AvailableSeats">Свободные места на момент публикации</param>
/// <param name="ChangeType">Тип изменения</param>
public sealed record EventChanged(
    Guid Id,
    string Title,
    DateTime StartAt,
    DateTime EndAt,
    int TotalSeats,
    int AvailableSeats,
    EventChangeType ChangeType
);
