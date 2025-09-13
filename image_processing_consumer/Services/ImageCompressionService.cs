using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using ImageProcessingConsumer.Models;
using Microsoft.Extensions.Logging;

namespace ImageProcessingConsumer.Services;

public interface IImageCompressionService
{
    Task<string> CompressImageAsync(ImageProcessingMessage message);
}

public class ImageCompressionService : IImageCompressionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageCompressionService> _logger;
    private readonly string _outputDirectory;

    public ImageCompressionService(HttpClient httpClient, ILogger<ImageCompressionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "compressed_images");
        
        // Ensure output directory exists
        Directory.CreateDirectory(_outputDirectory);
    }

    public async Task<string> CompressImageAsync(ImageProcessingMessage message)
    {
        try
        {
            _logger.LogInformation("Starting compression for image: {Url}", message.Url);

            // Download the image
            var imageBytes = await DownloadImageAsync(message.Url);
            
            // Generate output filename
            var fileName = GetCompressedFileName(message.OriginalFileName);
            var outputPath = Path.Combine(_outputDirectory, fileName);

            // Compress and save the image
            using var image = Image.Load(imageBytes);
            
            // Apply compression settings for PNG with deflate
            var encoder = new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestCompression,
                FilterMethod = PngFilterMethod.Adaptive
            };

            // Optionally resize for additional compression
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size((int)(image.Width * 0.8), (int)(image.Height * 0.8)),
                Mode = ResizeMode.Max
            }));

            await image.SaveAsync(outputPath, encoder);

            var originalSize = imageBytes.Length;
            var compressedSize = new FileInfo(outputPath).Length;
            var compressionRatio = ((double)(originalSize - compressedSize) / originalSize) * 100;

            _logger.LogInformation(
                "Image compressed successfully. Original: {OriginalSize} bytes, Compressed: {CompressedSize} bytes, Ratio: {Ratio:F2}%",
                originalSize, compressedSize, compressionRatio);

            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compressing image: {Url}", message.Url);
            throw;
        }
    }

    private async Task<byte[]> DownloadImageAsync(string imageUrl)
    {
        try
        {
            _logger.LogInformation("Downloading image from: {Url}", imageUrl);
            var response = await _httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();
            
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            _logger.LogInformation("Downloaded {Size} bytes", imageBytes.Length);
            
            return imageBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download image from: {Url}", imageUrl);
            throw;
        }
    }

    private static string GetCompressedFileName(string originalFileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return $"{nameWithoutExtension}_compressed_{timestamp}.png";
    }
}
