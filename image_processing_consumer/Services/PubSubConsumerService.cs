using Google.Cloud.PubSub.V1;
using System.Text.Json;
using ImageProcessingConsumer.Models;
using ImageProcessingConsumer.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ImageProcessingConsumer.Services;

public class PubSubConsumerService : BackgroundService
{
    private readonly ILogger<PubSubConsumerService> _logger;
    private readonly IImageCompressionService _compressionService;
    private readonly SubscriberClient _subscriberClient;
    private readonly string _subscriptionName;

    public PubSubConsumerService(
        ILogger<PubSubConsumerService> logger,
        IImageCompressionService compressionService,
        SubscriberClient subscriberClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _compressionService = compressionService;
        _subscriberClient = subscriberClient;
        _subscriptionName = configuration["PubSub:SubscriptionName"] 
            ?? throw new InvalidOperationException("PubSub:SubscriptionName configuration is required");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Pub/Sub consumer service for subscription: {SubscriptionName}", _subscriptionName);

        try
        {
            await _subscriberClient.StartAsync((message, cancellationToken) =>
            {
                return ProcessMessageAsync(message, cancellationToken);
            });

            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Pub/Sub consumer service stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Pub/Sub consumer service");
            throw;
        }
        finally
        {
            await _subscriberClient.StopAsync(TimeSpan.FromSeconds(30));
            _logger.LogInformation("Pub/Sub consumer service stopped");
        }
    }

    private async Task<SubscriberClient.Reply> ProcessMessageAsync(PubsubMessage message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received message with ID: {MessageId}", message.MessageId);

            // Deserialize the message
            var messageText = message.Data.ToStringUtf8();
            var imageProcessingMessage = JsonSerializer.Deserialize<ImageProcessingMessage>(messageText, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (imageProcessingMessage == null)
            {
                _logger.LogWarning("Failed to deserialize message: {MessageText}", messageText);
                return SubscriberClient.Reply.Nack;
            }

            _logger.LogInformation("Processing {TaskType} task for image: {FileName}", 
                imageProcessingMessage.TaskType, imageProcessingMessage.OriginalFileName);

            // Process the image based on task type
            if (imageProcessingMessage.TaskType.Equals("compressing", StringComparison.OrdinalIgnoreCase))
            {
                var outputPath = await _compressionService.CompressImageAsync(imageProcessingMessage);
                _logger.LogInformation("Image compressed successfully and saved to: {OutputPath}", outputPath);
            }
            else
            {
                _logger.LogWarning("Unknown task type: {TaskType}", imageProcessingMessage.TaskType);
                return SubscriberClient.Reply.Nack;
            }

            return SubscriberClient.Reply.Ack;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {MessageId}", message.MessageId);
            return SubscriberClient.Reply.Nack;
        }
    }
}
