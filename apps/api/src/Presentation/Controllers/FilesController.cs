using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController(IHostEnvironment env) : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { title = "Chưa chọn file." });

        var uploadsRoot = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsRoot);

        var ext = Path.GetExtension(file.FileName);
        var safeFileName = $"{System.Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsRoot, safeFileName);

        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream, ct);

        var url = $"/uploads/{safeFileName}";
        return Ok(new { url });
    }
}
