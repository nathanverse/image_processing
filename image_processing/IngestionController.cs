using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using image_processing.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace image_processing;

// IngestionController.cs

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly PublisherClient _publisherClient;
    private readonly IStorageService _storageService;
    
    public IngestionController(PublisherClient publisherClient, IStorageService storageService)
    {
        _publisherClient = publisherClient;
        _storageService = storageService;
    }

    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            return BadRequest("No image file provided.");
        }

        try
        {
            // 1. Upload the image to Google Cloud Storage
            var imageUrl = await _storageService.UploadImageAsync(imageFile);

            // 2. Create a message payload for Pub/Sub
            var messagePayload = new
            {
                Url = imageUrl,
                TaskType = "ocr",
                OriginalFileName = imageFile.FileName,
            };
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            // 3. Send the message to Pub/Sub
            var message = new PubsubMessage
            {
                Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(messagePayload, options))
            };
            
            await _publisherClient.PublishAsync(message);

            // 4. Return a success response
            return Ok(new { Message = "Image uploaded and processing task sent to Pub/Sub.", ImageUrl = imageUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error processing image upload", Error = ex.Message });
        }
    }

    [HttpGet("image/{fileName}")]
    public async Task<IActionResult> GetImage(string fileName)
    {
        try
        {
            // Add authorization check here later
            // if (!User.Identity.IsAuthenticated) return Unauthorized();
            
            var imageStream = await _storageService.DownloadImageAsync(fileName);
            
            // Determine content type based on file extension
            var contentType = GetContentType(fileName);
            
            return File(imageStream, contentType);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound($"Image not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving image: {ex.Message}");
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
}