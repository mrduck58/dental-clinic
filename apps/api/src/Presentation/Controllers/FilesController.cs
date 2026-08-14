using DentalClinic.API.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController(IFileStorageService fileStorage) : ControllerBase
{
    /// <summary>
    /// Chỉ nhận đúng các định dạng ảnh dùng để hiển thị. Trước đây đuôi file lấy thẳng từ tên client
    /// gửi lên, không kiểm gì — tải lên một file .html là có script chạy trên chính origin phục vụ
    /// ảnh, đọc được localStorage của trang quản trị. Danh sách trắng chặn đúng chuyện đó.
    ///
    /// CÓ .svg vì icon dịch vụ dùng định dạng này. Nó vẫn an toàn ở đây dù SVG nhúng được thẻ script:
    /// script trong SVG KHÔNG chạy khi ảnh được nạp qua thẻ &lt;img&gt; (chỉ chạy khi mở trực tiếp như
    /// một tài liệu), mà cả admin lẫn mobile đều hiển thị bằng &lt;img&gt;. Thêm nữa file nay nằm trên
    /// domain Supabase, khác origin với trang quản trị, nên có mở trực tiếp cũng không đọc được
    /// phiên đăng nhập của ai.
    /// </summary>
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp", "image/gif", "image/svg+xml" };

    private const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>
    /// POST api/files/upload — dùng chung cho mọi vai trò đã đăng nhập (Admin/Staff tải ảnh
    /// dịch vụ-thuốc-nhân viên, bệnh nhân tải ảnh đại diện...).
    ///
    /// File đi thẳng lên kho lưu trữ ngoài (Supabase Storage) khi được cấu hình; chỉ máy dev mới ghi
    /// xuống đĩa. Ổ đĩa của Render là ephemeral nên ghi đĩa ở đó là mất file sau mỗi lần restart.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [Authorize]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { title = "Chưa chọn file." });

        if (file.Length > MaxBytes)
            return BadRequest(new { title = $"Ảnh vượt quá {MaxBytes / 1024 / 1024}MB." });

        var ext = Path.GetExtension(file.FileName);

        // Kiểm CẢ đuôi file lẫn content-type: đuôi quyết định cách trình duyệt xử lý khi mở trực tiếp,
        // còn content-type là thứ client tự khai nên không thể tin một mình.
        if (!AllowedExtensions.Contains(ext) || !AllowedContentTypes.Contains(file.ContentType))
            return BadRequest(new
            {
                title = $"Chỉ chấp nhận ảnh {string.Join(", ", AllowedExtensions)}."
            });

        await using var stream = file.OpenReadStream();
        var url = await fileStorage.SaveAsync(stream, file.FileName, file.ContentType, ct);

        return Ok(new { url });
    }
}
