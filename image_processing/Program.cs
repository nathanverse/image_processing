// See https://aka.ms/new-console-template for more information
// using ImageHash;
using Confluent.Kafka;
using Google.Api.Gax;
using Google.Cloud.PubSub.V1;
using image_processing.Data;
using image_processing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Get configuration values
var projectId = builder.Configuration["PubSub:ProjectId"] ?? "bustling-icon-430107-b3"; // Replace with your actual project ID
var topicId = builder.Configuration["PubSub:TopicName"] ?? "image-processing-events"; // This should match your Terraform topic name
var topicName = TopicName.FromProjectTopic(projectId, topicId);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Register Storage Service
builder.Services.AddSingleton<IStorageService, StorageService>();

builder.Services.AddSingleton<PublisherClient>(provider =>
{
    var clientBuilder = new PublisherClientBuilder
    {
        TopicName = topicName,
        EmulatorDetection = EmulatorDetection.EmulatorOrProduction, // Automatically detects the Pub/Sub emulator
    };
    return clientBuilder.Build();
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));
var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
