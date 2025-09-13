# Image Processing Consumer Service

This is a standalone .NET console application that consumes messages from Google Cloud Pub/Sub and processes image compression tasks.

## Features

- Consumes messages from Google Cloud Pub/Sub
- Downloads images from URLs
- Compresses PNG images using deflate compression
- Saves compressed images to local directory
- Runs as a background service with proper logging

## Setup

1. **Configure Google Cloud Authentication**:
   ```bash
   # Set up Application Default Credentials
   gcloud auth application-default login
   
   # Or set environment variable for service account
   export GOOGLE_APPLICATION_CREDENTIALS="/path/to/your/service-account-key.json"
   ```

2. **Update Configuration**:
   Edit `appsettings.json` with your project details:
   ```json
   {
     "PubSub": {
       "ProjectId": "your-actual-project-id",
       "SubscriptionName": "image-processing-events-subscription"
     }
   }
   ```

3. **Install Dependencies**:
   ```bash
   cd image_processing_consumer
   dotnet restore
   ```

## Running the Service

### Development Mode
```bash
dotnet run
```

### Production Mode
```bash
# Build the application
dotnet build -c Release

# Run the built application
dotnet run -c Release
```

### As a Service
```bash
# Publish self-contained executable
dotnet publish -c Release -r linux-x64 --self-contained

# Run the published executable
./bin/Release/net9.0/linux-x64/publish/image_processing_consumer
```

## Output

- Compressed images are saved to `./compressed_images/` directory
- Each compressed image is named with the pattern: `{original_name}_compressed_{timestamp}.png`
- The service logs compression ratios and file sizes

## Configuration Options

### appsettings.json
- `PubSub:ProjectId`: Your Google Cloud Project ID
- `PubSub:SubscriptionName`: The Pub/Sub subscription name
- `Logging:LogLevel`: Logging configuration

### Environment Variables
You can override configuration using environment variables:
- `PubSub__ProjectId`
- `PubSub__SubscriptionName`

## Message Format

The service expects messages in this JSON format:
```json
{
  "url": "https://example.com/image.png",
  "taskType": "compressing",
  "originalFileName": "image.png"
}
```

## Stopping the Service

Press `Ctrl+C` to gracefully stop the service. It will:
1. Stop accepting new messages
2. Complete processing current messages
3. Properly close Pub/Sub connections
