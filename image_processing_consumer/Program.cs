using Google.Cloud.PubSub.V1;
using ImageProcessingConsumer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Add HttpClient for downloading images
builder.Services.AddHttpClient<IImageCompressionService, ImageCompressionService>();

// Add services
builder.Services.AddSingleton<IImageCompressionService, ImageCompressionService>();

// Configure Pub/Sub Subscriber
builder.Services.AddSingleton<SubscriberClient>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var projectId = configuration["PubSub:ProjectId"] 
        ?? throw new InvalidOperationException("PubSub:ProjectId configuration is required");
    var subscriptionName = configuration["PubSub:SubscriptionName"]
        ?? throw new InvalidOperationException("PubSub:SubscriptionName configuration is required");

    var subscriptionPath = SubscriptionName.FromProjectSubscription(projectId, subscriptionName);
    
    var clientBuilder = new SubscriberClientBuilder
    {
        SubscriptionName = subscriptionPath,
        Settings = new SubscriberSettings
        {
            AckExtensionWindow = TimeSpan.FromMinutes(5),
            FlowControlSettings = new Google.Api.Gax.FlowControlSettings(
                maxOutstandingElementCount: 100,
                maxOutstandingByteCount: null
            )
        }
    };

    return clientBuilder.Build();
});

// Add the background service
builder.Services.AddHostedService<PubSubConsumerService>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Image Processing Consumer Service...");

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}
