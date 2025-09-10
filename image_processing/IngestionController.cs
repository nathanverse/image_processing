using Confluent.Kafka;

namespace image_processing;

// IngestionController.cs

using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly IProducer<string, string> _kafkaProducer;

    public IngestionController(IProducer<string, string> kafkaProducer)
    {
        _kafkaProducer = kafkaProducer;
    }

    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile imageFile)
    {
        if (imageFile.Length == 0)
        {
            return BadRequest("No image file provided.");
        }

        var imageUrl = "https://ahayne.com/wp-content/uploads/2024/10/anime-girl-14pcSwFB.jpg";

        // 3. Create a message payload for Kafka
        var messagePayload = new
        {
            Url = imageUrl,
            TaskType = "compressing",
            OriginalFileName = imageFile.FileName,
        };

        // 4. Send the message to Kafka
        var message = new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(), // Use a unique key
            Value = JsonSerializer.Serialize(messagePayload)
        };

        await _kafkaProducer.ProduceAsync("image-processing-topic", message);

        // 5. Return a success response
        return Ok(new { Message = "Image ingested successfully and processing task sent to Kafka.", ImageUrl = imageUrl });
    }
}