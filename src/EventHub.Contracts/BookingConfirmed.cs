namespace EventHub.Contracts;

/// <summary>
/// Событие «бронь подтверждена» — контракт сообщения топика booking-confirmed
/// </summary>
/// <param name="BookingId">Идентификатор брони</param>
/// <param name="EventId">Идентификатор события</param>
/// <param name="UserId">Идентификатор пользователя</param>
/// <param name="Seats">Количество занятых мест</param>
/// <param name="ConfirmedAt">Момент подтверждения брони (UTC)</param>
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int Seats,
    DateTime ConfirmedAt
);
