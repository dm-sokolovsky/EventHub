using System.Text.Json;
using BookingsService.Application;
using Confluent.Kafka;
using EventHub.Contracts;
using Microsoft.Extensions.Configuration;

namespace BookingsService.Infrastructure;

public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{

    private readonly IProducer<string, string> _producer;

    public KafkaEventPublisher(IConfiguration configuration)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
        };
        
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }
    
    public async Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);
        await _producer.ProduceAsync(
            Topics.BookingConfirmed,
            new Message<string, string>
            {
                Key = message.EventId.ToString(),
                Value = json
            }, cancellationToken
        );
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}