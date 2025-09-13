using Confluent.Kafka;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;

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
    private readonly PublisherClient _publisherClient;
    public IngestionController(PublisherClient publisherClient)
    {
        _publisherClient = publisherClient;
    }

    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile imageFile)
    {
        if (imageFile.Length == 0)
        {
            return BadRequest("No image file provided.");
        }

        var imageUrl = "https://ahayne.com/wp-content/uploads/2024/10/anime-girl-14pcSwFB.jpg";

        // 3. Create a message payload for Pub/Sub
        var messagePayload = new
        {
            Url = imageUrl,
            TaskType = "compressing",
            OriginalFileName = imageFile.FileName,
        };

        // 4. Send the message to Pub/Sub
        var message = new PubsubMessage
        {
            Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(messagePayload))
        };
        
        await _publisherClient.PublishAsync(message);

        // 5. Return a success response
        return Ok(new { Message = "Image ingested successfully and processing task sent to Pub/Sub.", ImageUrl = imageUrl });
    }
}