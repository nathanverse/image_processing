using Google;
using Google.Cloud.Storage.V1;
using Google.Apis;

namespace image_processing.Services
{
    public interface IStorageService
    {
        Task<string> UploadImageAsync(IFormFile imageFile, string? fileName = null);
        Task<Stream> DownloadImageAsync(string fileName);
    }

    public class StorageService : IStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;

        public StorageService(IConfiguration configuration)
        {
            _storageClient = StorageClient.Create();
            _bucketName = configuration["Storage:BucketName"] ?? throw new ArgumentException("Storage:BucketName not configured");
        }

        public async Task<string> UploadImageAsync(IFormFile imageFile, string? fileName = null)
        {
            if (imageFile == null || imageFile.Length == 0)
                throw new ArgumentException("Invalid image file");

            // Generate unique filename if not provided
            if (string.IsNullOrEmpty(fileName))
            {
                var extension = Path.GetExtension(imageFile.FileName);
                fileName = $"images/{Guid.NewGuid()}{extension}";
            }

            // Upload to Google Cloud Storage
            using var stream = imageFile.OpenReadStream();
            await _storageClient.UploadObjectAsync(
                bucket: _bucketName,
                objectName: fileName,
                contentType: imageFile.ContentType,
                source: stream
            );

            // Return your app's URL instead of direct GCS URL
            var fileNameOnly = fileName.StartsWith("images/") ? fileName.Substring(7) : fileName;
            return $"/api/ingestion/image/{fileNameOnly}";
        }

        public async Task<Stream> DownloadImageAsync(string fileName)
        {
            try
            {
                // Ensure the file is in the images folder
                var objectName = fileName.StartsWith("images/") ? fileName : $"images/{fileName}";
                
                var memoryStream = new MemoryStream();
                await _storageClient.DownloadObjectAsync(_bucketName, objectName, memoryStream);
                memoryStream.Position = 0;
                
                return memoryStream;
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new FileNotFoundException($"Image {fileName} not found in bucket");
            }
        }
    }
}
