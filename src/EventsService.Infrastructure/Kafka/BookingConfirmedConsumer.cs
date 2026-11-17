using System.Text.Json;
using Confluent.Kafka;
using EventHub.Contracts;
using EventsService.Application.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventsService.Infrastructure;

public sealed class BookingConfirmedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;                 
    private readonly IServiceScopeFactory _scopeFactory;                  
    private readonly ILogger<BookingConfirmedConsumer> _logger;  
    
    public BookingConfirmedConsumer(                                      
        IConfiguration configuration,                                     
        IServiceScopeFactory scopeFactory,                                
        ILogger<BookingConfirmedConsumer> logger)                        
    {            
        _scopeFactory = scopeFactory;
        _logger = logger;
        
        var config = new ConsumerConfig                                   
        {                                                                 
            BootstrapServers = configuration["Kafka:BootstrapServers"],   
            GroupId = configuration["Kafka:ConsumerGroup"],               
            AutoOffsetReset = AutoOffsetReset.Earliest                                   
        };                                                                
        _consumer = new ConsumerBuilder<string, string>(config).Build();  
        _consumer.Subscribe(Topics.BookingConfirmed);  
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
                    _logger.LogError(ex, "Consume error in topic {Topic}, retrying", Topics.BookingConfirmed);
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
        BookingConfirmed message;
        try
        {
            message = JsonSerializer.Deserialize<BookingConfirmed>(json)
                      ?? throw new JsonException("BookingConfirmed is null");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"Cannot deserialize message: {json}");
            return;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, ex.Message);
            return;
        }
        
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        
        var @event = await repo.GetByIdAsync(message.EventId, stoppingToken);
        if (@event is null)
        {
            _logger.LogWarning($"Event {message.EventId} not found, message skipped"); 
            return;
        }

        if (!@event.TryReserveSeats(message.Seats))
        {
            _logger.LogWarning($"Event {message.EventId} is reserved, skipping");
            return;
        }

        await repo.SaveChangesAsync(stoppingToken);
        _logger.LogInformation($"Event {message.EventId}: reserved {message.Seats} for booking {message.BookingId}");
    }
}