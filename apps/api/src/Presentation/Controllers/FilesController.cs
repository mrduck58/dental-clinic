using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController(IHostEnvironment env) : ControllerBase
{
    /// <summary>POST api/files/upload — dùng chung cho mọi vai trò đã đăng nhập (Admin/Staff tải ảnh
    /// dịch vụ-thuốc-nhân viên, bệnh nhân tải ảnh đại diện...) — trước đây không yêu cầu đăng nhập,
    /// ai cũng gọi được kể cả ẩn danh.</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [Authorize]
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
