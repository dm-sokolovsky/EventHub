using System.Text.Json;
using Confluent.Kafka;
using EventHub.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingsService.Infrastructure.Kafka;

/// <summary>
/// Подписчик топика events-changed: поддерживает локальную копию событий
/// EventsService в таблице cd_local_events
/// </summary>
public sealed class EventsChangedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<EventsChangedConsumer> _logger;

    public EventsChangedConsumer(
        IConfiguration configuration,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ILogger<EventsChangedConsumer> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            GroupId = configuration["Kafka:ConsumerGroup"],
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(Topics.EventsChanged);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = _consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    // Ошибка брокера/топика не должна останавливать сервис
                    _logger.LogError(ex, "Consume error in topic {Topic}, retrying", Topics.EventsChanged);
                    continue;
                }

                if (result?.Message?.Value is null)
                    continue;

                await HandleAsync(result.Message.Value, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            _consumer.Close();
        }
    }

    private async Task HandleAsync(string json, CancellationToken stoppingToken)
    {
        EventChanged message;
        try
        {
            message = JsonSerializer.Deserialize<EventChanged>(json)
                      ?? throw new JsonException("EventChanged is null");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Cannot deserialize message, skipped: {Json}", json);
            return;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(stoppingToken);

        if (message.ChangeType == EventChangeType.Deleted)
        {
            var deleted = await db.LocalEvents
                .Where(e => e.Id == message.Id)
                .ExecuteDeleteAsync(stoppingToken);
            if (deleted > 0)
                _logger.LogInformation("Local event {EventId} deleted", message.Id);
            return;
        }

        var localEvent = await db.LocalEvents.FirstOrDefaultAsync(e => e.Id == message.Id, stoppingToken);

        if (localEvent is null)
        {
            db.LocalEvents.Add(new Models.LocalEvent
            {
                Id = message.Id,
                Title = message.Title,
                StartAt = message.StartAt,
                EndAt = message.EndAt,
                TotalSeats = message.TotalSeats,
                AvailableSeats = message.AvailableSeats
            });
        }
        else
        {
            var totalSeatsDelta = message.TotalSeats - localEvent.TotalSeats;

            localEvent.Title = message.Title;
            localEvent.StartAt = message.StartAt;
            localEvent.EndAt = message.EndAt;
            localEvent.TotalSeats = message.TotalSeats;
            localEvent.AvailableSeats = Math.Clamp(localEvent.AvailableSeats + totalSeatsDelta, 0, message.TotalSeats);
        }

        await db.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("Local event {EventId} upserted ({ChangeType})", message.Id, message.ChangeType);
    }
}
