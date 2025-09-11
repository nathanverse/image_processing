// See https://aka.ms/new-console-template for more information
// using ImageHash;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text.Json;
using Confluent.Kafka;
using Google.Api.Gax;
using Google.Cloud.PubSub.V1;

var builder = WebApplication.CreateBuilder(args);
var topicId = "your-topic-id"; // Replace with your Pub/Sub topic ID
var projectId = "your-gcp-project-id"; // Replace with your Google Cloud project ID
var topicName = TopicName.FromProjectTopic(projectId, topicId);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddSingleton<PublisherClient>(provider =>
{
    var clientBuilder = new PublisherClientBuilder
    {
        TopicName = topicName,
        EmulatorDetection = EmulatorDetection.EmulatorOrProduction, // Automatically detects the Pub/Sub emulator
    };
    return clientBuilder.Build();
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
