using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace image_processing.Services
{
    public interface IStorageService
    {
        Task<string> UploadImageAsync(IFormFile imageFile, string? fileName = null);
        Task<Stream> DownloadImageAsync(string fileName);
        Task<bool> ObjectExistsAsync(string objectName);
        Task<string> GetImageUrlAsync(string objectName);
    }

    public class StorageService : IStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly string _cloudName;

        public StorageService(IConfiguration configuration)
        {
            _cloudName = configuration["Cloudinary:CloudName"]
                         ?? throw new ArgumentException("Cloudinary:CloudName not configured");

            var apiKey = configuration["Cloudinary:ApiKey"]
                         ?? throw new ArgumentException("Cloudinary:ApiKey not configured");

            var apiSecret = configuration["Cloudinary:ApiSecret"]
                            ?? throw new ArgumentException("Cloudinary:ApiSecret not configured");

            var account = new Account(_cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<bool> ObjectExistsAsync(string objectName)
        {
            try
            {
                var getParams = new GetResourceParams(objectName)
                {
                    ResourceType = ResourceType.Image
                };
                var result = await _cloudinary.GetResourceAsync(getParams);
                return result.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch
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
                fileName = $"deduped/{Guid.NewGuid()}{extension}";
            }

            await using var stream = imageFile.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(imageFile.FileName, stream),
                PublicId = fileName,
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            if (uploadResult.Error != null)
                throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<Stream> DownloadImageAsync(string fileName)
        {
            var url = await GetImageUrlAsync(fileName);
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new FileNotFoundException($"Image not found on Cloudinary: {fileName}");

            var memoryStream = new MemoryStream();
            await response.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public Task<string> GetImageUrlAsync(string objectName)
        {
            // Cloudinary URL chuẩn
            var url = $"https://res.cloudinary.com/{_cloudName}/image/upload/{objectName}";
            return Task.FromResult(url);
        }
    }
}