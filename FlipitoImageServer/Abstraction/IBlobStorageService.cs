

using Azure.Storage.Blobs;

namespace FlipitoImageServer.Abstraction
{
    public interface IBlobStorageService
    {
        Task<string> UploadImageAsync(string type, IFormFile file);
        Task<List<string>> UploadImagesAsync(string type, List<IFormFile> files);
        BlobClient GetImageBlob(string type, string fileName);
    }
}
