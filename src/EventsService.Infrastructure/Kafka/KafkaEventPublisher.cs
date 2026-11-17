using System.Text.Json;
using Confluent.Kafka;
using EventHub.Contracts;
using EventsService.Application;
using EventsService.Application.Exceptions;
using Microsoft.Extensions.Configuration;
namespace EventsService.Infrastructure.Kafka;

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

    public async Task PublishAsync(EventChanged message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);
        try
        {
            await _producer.ProduceAsync(
                Topics.EventsChanged,
                new Message<string, string>
                {
                    Key = message.Id.ToString(),
                    Value = json
                }, cancellationToken
            );
        }
        catch (KafkaException ex)
        {
            throw new EventPublishingException(
                $"Failed to publish event {message.Id} to Kafka",
                ex);
        }
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}
