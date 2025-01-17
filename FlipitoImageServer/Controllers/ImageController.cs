using Microsoft.AspNetCore.Mvc;
using FlipitoImageServer.Abstraction;

[ApiController]
[Route("api/image")]
public class ImagesController : ControllerBase
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly string[] _allowedTypes = { "business", "user" };

    public ImagesController(IBlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
    }

    [HttpPost("upload/{type}/image")]
    public async Task<IActionResult> UploadImage(string type, IFormFile file)
    {
        if (!_allowedTypes.Contains(type.ToLower()))
            return BadRequest("Invalid type. Use 'business' or 'user'.");

        try
        {
            var fileName = await _blobStorageService.UploadImageAsync(type, file);
            return Ok(new { FileName = fileName });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while uploading the image.");
        }
    }

    [HttpPost("upload/{type}/images")]
    public async Task<IActionResult> UploadImages(string type, List<IFormFile> files)
    {
        if (!_allowedTypes.Contains(type.ToLower()))
            return BadRequest("Invalid type. Use 'business' or 'user'.");

        try
        {
            var fileNames = await _blobStorageService.UploadImagesAsync(type, files);
            return Ok(fileNames);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while uploading the images.");
        }
    }

    [HttpGet("{type}/{fileName}")]
    public IActionResult GetImage(string type, string fileName)
    {
        if (!_allowedTypes.Contains(type.ToLower()))
            return BadRequest("Invalid type. Use 'business' or 'user'.");

        var blobClient = _blobStorageService.GetImageBlob(type, fileName);
        if (!blobClient.Exists())
            return NotFound("Image not found.");

        return Redirect(blobClient.Uri.ToString());
    }
}
