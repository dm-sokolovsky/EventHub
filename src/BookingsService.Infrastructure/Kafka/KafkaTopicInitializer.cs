using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EventHub.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingsService.Infrastructure.Kafka;

/// <summary>
/// Создаёт топики при старте, если их ещё нет.
/// Не валит запуск приложения при ошибке — подписчики попробуют позже.
/// </summary>
public sealed class KafkaTopicInitializer : BackgroundService
{
    private static readonly string[] RequiredTopics =
    [
        Topics.BookingConfirmed,
        Topics.EventsChanged
    ];

    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(
        IConfiguration configuration,
        ILogger<KafkaTopicInitializer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var admin = new AdminClientBuilder(
                new AdminClientConfig
                {
                    BootstrapServers = _configuration["Kafka:BootstrapServers"]
                }).Build();

            var metadata = admin.GetMetadata(TimeSpan.FromSeconds(10));

            foreach (var topic in RequiredTopics)
            {
                if (metadata.Topics.Any(t => t.Topic == topic))
                {
                    _logger.LogInformation("Topic {Topic} already exists", topic);
                    continue;
                }

                await admin.CreateTopicsAsync(new[]
                {
                    new TopicSpecification
                    {
                        Name = topic,
                        NumPartitions = 3,
                        ReplicationFactor = 1,
                    }
                });

                _logger.LogInformation("Created topic: {Topic}", topic);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Kafka topics");
        }
    }
}
