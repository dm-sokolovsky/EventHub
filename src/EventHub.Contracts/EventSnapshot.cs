namespace EventHub.Contracts;

/// <summary>
/// Срез данных события, передаваемый между сервисами
/// </summary>
/// <param name="Id">Id события</param>
/// <param name="StartAt">Дата и время начала события</param>
public sealed record EventSnapshot(Guid Id, DateTime StartAt);
