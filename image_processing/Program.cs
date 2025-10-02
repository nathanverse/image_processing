using Confluent.Kafka;
using image_processing.Data;
using image_processing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PubnubApi;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add configuration from both appsettings.json and appsettings.local.json
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Get configuration values
var pubnubConfig = new PNConfiguration("image-processing"); // Initialize with Uuid
pubnubConfig.PublishKey = builder.Configuration["PubNub:PublishKey"] ?? throw new ArgumentException("PubNub:PublishKey not configured");
pubnubConfig.SubscribeKey = builder.Configuration["PubNub:SubscribeKey"] ?? throw new ArgumentException("PubNub:SubscribeKey not configured");
var pubnubChannel = builder.Configuration["PubNub:Channel"] ?? "image-processing-channel";

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

// Register PubNub client
builder.Services.AddSingleton<Pubnub>(provider =>
{
    return new Pubnub(pubnubConfig);
});

builder.Services.AddDbContext<IngestionDBcontext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));
var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();