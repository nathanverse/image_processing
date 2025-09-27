using Google;
using Google.Cloud.Storage.V1;
using Google.Api.Gax;
using Microsoft.AspNetCore.Http;

namespace image_processing.Services
{
    public interface IStorageService
    {
        Task<string> UploadImageAsync(IFormFile imageFile, string? fileName = null);
        Task<Stream> DownloadImageAsync(string fileName);
        Task<bool> ObjectExistsAsync(string objectName);
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

        public async Task<bool> ObjectExistsAsync(string objectName)
        {
            try
            {
                await _storageClient.GetObjectAsync(_bucketName, objectName);
                return true;
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<string> UploadImageAsync(IFormFile imageFile, string? fileName = null)
        {
            if (imageFile == null || imageFile.Length == 0)
                throw new ArgumentException("Invalid image file");

            if (string.IsNullOrEmpty(fileName))
            {
                var extension = Path.GetExtension(imageFile.FileName);
                fileName = $"images/{Guid.NewGuid()}{extension}";
            }

            using var stream = imageFile.OpenReadStream();
            await _storageClient.UploadObjectAsync(
                _bucketName,
                fileName,
                imageFile.ContentType,
                stream
            );

            var fileNameOnly = fileName.StartsWith("images/") ? fileName.Substring(7) : fileName;
            return $"/api/ingestion/image/{fileNameOnly}";
        }

        public async Task<Stream> DownloadImageAsync(string fileName)
        {
            var objectName = fileName.StartsWith("images/") ? fileName : $"images/{fileName}";
            var memoryStream = new MemoryStream();
            await _storageClient.DownloadObjectAsync(_bucketName, objectName, memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
