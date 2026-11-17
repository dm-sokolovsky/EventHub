using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EventHub.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventsService.Infrastructure;

public sealed class KafkaTopicInitializer : IHostedService
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
        ILogger<KafkaTopicInitializer> logger
    )
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var admin = new AdminClientBuilder(
            new AdminClientConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"]
            }).Build();

        foreach (var topic in RequiredTopics)
        {
            try
            {
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
            catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                // Другой сервис уже создал топик — нормальная ситуация при параллельном старте
                _logger.LogInformation("Topic {Topic} already exists", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Kafka topic {Topic}", topic);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
