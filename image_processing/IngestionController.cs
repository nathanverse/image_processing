using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using image_processing.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Security.Cryptography;
using image_processing.Data;
using image_processing.Models;

namespace image_processing;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly PublisherClient _publisherClient;
    private readonly IStorageService _storageService;
    private readonly AppDbContext _dbContext; 

    public IngestionController(PublisherClient publisherClient, IStorageService storageService, AppDbContext dbContext)
    {
        _publisherClient = publisherClient;
        _storageService = storageService;
        _dbContext = dbContext;
    }

    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            return BadRequest("No image file provided.");

        try
        {
            // Compute SHA-256 hash for deduplication
            string hash;
            using (var stream = imageFile.OpenReadStream())
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = await sha256.ComputeHashAsync(stream);
                hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                stream.Position = 0;  
            }

            var extension = Path.GetExtension(imageFile.FileName);
            var objectName = $"deduped/{hash}{extension}"; 

          
            bool exists = await _storageService.ObjectExistsAsync(objectName);
            string imageUrl;
            if (!exists)
            {
                using var uploadStream = imageFile.OpenReadStream();
                imageUrl = await _storageService.UploadImageAsync(imageFile, objectName);
            }
            else
            {
                imageUrl = $"/api/ingestion/image/{hash}{extension}";
            }

            // Lưu task vào DB
            var task = new TaskModel
            {
                Id = Guid.NewGuid(),
                OriginUrl = imageUrl,
                OriginalFileName = imageFile.FileName,
                TaskType = "ocr"
            };
            _dbContext.Tasks.Add(task);
            await _dbContext.SaveChangesAsync();
            var messagePayload = new
            {
                TaskId = task.Id.ToString(),
                Url = imageUrl,
                TaskType = "ocr",
                OriginalFileName = imageFile.FileName
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
            var message = new PubsubMessage { Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(messagePayload, options)) };
            await _publisherClient.PublishAsync(message);

            return Ok(new { Message = "Image uploaded (or deduped) and processing task sent.", TaskId = task.Id, ImageUrl = imageUrl });
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