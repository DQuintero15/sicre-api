using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sicre.Api.Features.Assets.Controllers;

[ApiController]
[Route("api/assets")]
[AllowAnonymous]
public class AssetsController(IWebHostEnvironment env) : ControllerBase
{
    [HttpGet("images/{fileName}")]
    public IActionResult GetImage(string fileName)
    {
        var path = Path.Combine(env.ContentRootPath, "Assets", "Images", fileName);

        if (!System.IO.File.Exists(path))
            return NotFound();

        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        var contentType = extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream",
        };

        return PhysicalFile(path, contentType);
    }
}
