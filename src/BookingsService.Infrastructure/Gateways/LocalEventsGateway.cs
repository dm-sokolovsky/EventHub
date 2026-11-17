using BookingsService.Application.Services.Abstractions;
using EventHub.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookingsService.Infrastructure.Gateways;

/// <summary>
/// Шлюз к данным событий через локальную копию (read model),
/// которую поддерживает EventsChangedConsumer по сообщениям из EventsService
/// </summary>
public sealed class LocalEventsGateway(IDbContextFactory<AppDbContext> dbContextFactory) : IEventsGateway
{
    // Сериализуем изменение мест: BookingService допускает параллельные операции,
    // а читать-изменить-записать через EF не атомарно
    private static readonly SemaphoreSlim SeatsLock = new(1, 1);

    public async Task<EventSnapshot?> GetAsync(Guid eventId, CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var localEvent = await db.LocalEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        return localEvent is null
            ? null
            : new EventSnapshot(localEvent.Id, localEvent.StartAt);
    }

    public async Task<bool> TryReserveSeatsAsync(Guid eventId, CancellationToken ct = default)
    {
        await SeatsLock.WaitAsync(ct);
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            var localEvent = await db.LocalEvents.FirstOrDefaultAsync(e => e.Id == eventId, ct);
            if (localEvent is null || localEvent.AvailableSeats < 1)
                return false;

            localEvent.AvailableSeats--;
            await db.SaveChangesAsync(ct);
            return true;
        }
        finally
        {
            SeatsLock.Release();
        }
    }

    public async Task ReleaseSeatsAsync(Guid eventId, CancellationToken ct = default)
    {
        await SeatsLock.WaitAsync(ct);
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            var localEvent = await db.LocalEvents.FirstOrDefaultAsync(e => e.Id == eventId, ct);
            if (localEvent is null)
                return;

            localEvent.AvailableSeats = Math.Min(localEvent.TotalSeats, localEvent.AvailableSeats + 1);
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            SeatsLock.Release();
        }
    }
}
