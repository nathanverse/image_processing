using image_processing.Data;
using image_processing.Data.Models.Entities;
using image_processing.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PubnubApi;
using System.Security.Cryptography;
using System.Text.Json;

namespace image_processing.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly Pubnub _pubnub;
    private readonly IStorageService _storageService;
    private readonly IngestionDBcontext _dbContext;
    private readonly IConfiguration _configuration; // Để lấy channel

    public IngestionController(
        Pubnub pubnub,
        IStorageService storageService,
        IngestionDBcontext dbContext,
        IConfiguration configuration)
    {
        _pubnub = pubnub;
        _storageService = storageService;
        _dbContext = dbContext;
        _configuration = configuration;
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

            string imageUrl;
            if (!await _storageService.ObjectExistsAsync(objectName))
            {
                using var uploadStream = imageFile.OpenReadStream();
                imageUrl = await _storageService.UploadImageAsync(imageFile, objectName);
            }
            else
            {
                // Không build thủ công → lấy URL từ storage service
                imageUrl = await _storageService.GetImageUrlAsync(objectName);
            }

            var existingTask = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.OriginUrl == imageUrl);
            if (existingTask != null)
            {
                return Ok(new
                {
                    Message = "Image already processed.",
                    TaskId = existingTask.Id,
                    ImageUrl = imageUrl
                });
            }

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
            var serializedMessage = JsonSerializer.Serialize(messagePayload, options);

            var publishResult = await _pubnub.Publish()
                .Channel(_configuration["PubNub:Channel"] ?? "image-processing-channel")
                .Message(serializedMessage)
                .ExecuteAsync();

            if (publishResult.Result == null || publishResult.Status.Error)
                throw new Exception("Failed to publish message to PubNub");

            return Ok(new
            {
                Message = "Image uploaded (or deduped) and processing task sent.",
                TaskId = task.Id,
                ImageUrl = imageUrl
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Message = "Error processing image upload",
                Error = ex.Message
            });
        }
    }

    [HttpGet("image/{fileName}")]
    public async Task<IActionResult> GetImage(string fileName)
    {
        try
        {
            var imageStream = await _storageService.DownloadImageAsync(fileName);
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
