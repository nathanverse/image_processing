namespace ImageProcessingConsumer.Models;

public class ImageProcessingMessage
{
    public string Url { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
}
