namespace image_processing.Models;

public class TaskModel
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "INIT";
    public string? FailureReason { get; set; }
    public string OriginUrl { get; set; } = string.Empty;
    public string? OutputUrl { get; set; }
    public string TaskType { get; set; } = "ocr";  
    public string OriginalFileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}