using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly BlobContainerClient _blobContainer;

    public ImagesController(IConfiguration configuration)
    {
        var containerName = configuration["AzureBlobStorage:ContainerName"];
        var connectionString = configuration["AzureBlobStorage:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException("AzureBlobStorage:ConnectionString");

        var blobServiceClient = new BlobServiceClient(connectionString);

        _blobContainer = blobServiceClient.GetBlobContainerClient(containerName);

        _blobContainer.CreateIfNotExists();
    }

    [HttpPost("upload/{type}")]
    public async Task<IActionResult> UploadImage(string type, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var allowedTypes = new[] { "business", "user" };
        if (!Array.Exists(allowedTypes, t => t.Equals(type, StringComparison.OrdinalIgnoreCase)))
            return BadRequest("Invalid type. Use 'business' or 'user'.");

        var blobName = $"{type}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var blobClient = _blobContainer.GetBlobClient(blobName);

        using (var stream = file.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
        }

        return Ok(new { Url = blobClient.Uri.ToString() });
    }

    [HttpGet("{type}/{fileName}")]
    public IActionResult GetImage(string type, string fileName)
    {
        var blobName = $"{type}/{fileName}";
        var blobClient = _blobContainer.GetBlobClient(blobName);

        if (!blobClient.Exists())
            return NotFound("Image not found.");

        return Redirect(blobClient.Uri.ToString());
    }
}
