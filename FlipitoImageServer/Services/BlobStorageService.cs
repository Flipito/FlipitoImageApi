using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using Azure.Storage.Blobs;
using FlipitoImageServer.Abstraction;
using Azure.Storage.Blobs.Models;

namespace FlipitoImageServer.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _blobContainer;
        private readonly string[] _allowedMimeTypes = { "image/jpeg", "image/png", "image/webp" };
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public BlobStorageService(string connectionString, string containerName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException("Blob Storage connection string cannot be null.");

            var blobServiceClient = new BlobServiceClient(connectionString);
            _blobContainer = blobServiceClient.GetBlobContainerClient(containerName);
            _blobContainer.CreateIfNotExists();
        }

        public async Task<string> UploadImageAsync(string type, IFormFile file)
        {
            if (!ValidateFile(file, out string validationMessage))
                throw new ArgumentException(validationMessage);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blobName = $"{type}/{fileName}";
            var blobClient = _blobContainer.GetBlobClient(blobName);

            using (var compressedStream = await CompressImageAsync(file))
            {
                await blobClient.UploadAsync(compressedStream, new BlobHttpHeaders { ContentType = file.ContentType });
            }

            return fileName; // Returning only the file name
        }

        public async Task<List<string>> UploadImagesAsync(string type, List<IFormFile> files)
        {
            var fileNames = new List<string>();

            foreach (var file in files)
            {
                if (!ValidateFile(file, out string validationMessage))
                    throw new ArgumentException(validationMessage);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var blobName = $"{type}/{fileName}";
                var blobClient = _blobContainer.GetBlobClient(blobName);

                using (var compressedStream = await CompressImageAsync(file))
                {
                    await blobClient.UploadAsync(compressedStream, new BlobHttpHeaders { ContentType = file.ContentType });
                }

                fileNames.Add(fileName); // Add file name instead of URL
            }

            return fileNames;
        }

        public BlobClient GetImageBlob(string type, string fileName)
        {
            return _blobContainer.GetBlobClient($"{type}/{fileName}");
        }

        private bool ValidateFile(IFormFile file, out string validationMessage)
        {
            validationMessage = string.Empty;

            if (file == null || file.Length == 0)
            {
                validationMessage = "No file uploaded.";
                return false;
            }

            if (file.Length > _maxFileSize)
            {
                validationMessage = $"File size exceeds {_maxFileSize / (1024 * 1024)}MB limit.";
                return false;
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                validationMessage = "Invalid file format. Allowed formats: .jpg, .jpeg, .png, .webp.";
                return false;
            }

            if (!_allowedMimeTypes.Contains(file.ContentType))
            {
                validationMessage = "Invalid file MIME type.";
                return false;
            }

            return true;
        }

        private async Task<MemoryStream> CompressImageAsync(IFormFile file)
        {
            using (var inputStream = file.OpenReadStream())
            using (var image = await Image.LoadAsync(inputStream))
            {
                int maxWidth = 1080;
                int maxHeight = 1080;
                bool shouldResize = image.Width > maxWidth || image.Height > maxHeight;

                var outputStream = new MemoryStream();
                var extension = Path.GetExtension(file.FileName).ToLower();

                if (extension == ".jpg" || extension == ".jpeg")
                {
                    // Apply JPEG compression
                    if (shouldResize)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(maxWidth, maxHeight)
                        }));
                    }
                    await image.SaveAsync(outputStream, new JpegEncoder { Quality = 75 });
                }
                else if (extension == ".png")
                {
                    if (shouldResize)
                    {
                        // Resize PNG if it's too large
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(maxWidth, maxHeight)
                        }));
                        await image.SaveAsync(outputStream, new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression });
                    }
                    else
                    {
                        // Keep the original PNG as is (no compression applied)
                        await file.CopyToAsync(outputStream);
                    }
                }
                else if (extension == ".webp")
                {
                    if (shouldResize)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(maxWidth, maxHeight)
                        }));
                    }
                    await image.SaveAsync(outputStream, new WebpEncoder { Quality = 75 });
                }
                else
                {
                    throw new NotSupportedException("Unsupported image format.");
                }

                outputStream.Position = 0;
                return outputStream;
            }
        }


    }
}
