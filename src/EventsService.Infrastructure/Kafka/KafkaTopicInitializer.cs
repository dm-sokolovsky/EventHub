using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EventHub.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventsService.Infrastructure;

public sealed class KafkaTopicInitializer : BackgroundService
{
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
            if (metadata.Topics.Any(t => t.Topic == Topics.BookingConfirmed))
            {
                _logger.LogInformation("Topic {Topic} already exists", Topics.BookingConfirmed);
                return;
            }

            await admin.CreateTopicsAsync(new[]
            {
                new TopicSpecification
                {
                    Name = Topics.BookingConfirmed,
                    NumPartitions = 3,
                    ReplicationFactor = 1,
                }
            });

            _logger.LogInformation("Created topic: {Topic}", Topics.BookingConfirmed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating topic");
        }
    }
}