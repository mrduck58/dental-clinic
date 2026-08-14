using System.Net.Http.Headers;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalClinic.API.Infrastructure.Services;

/// <summary>
/// Lưu file lên Supabase Storage thay vì đĩa của máy chủ ứng dụng.
///
/// Bắt buộc phải làm vậy khi chạy trên Render: ổ đĩa ở đó là ephemeral — mọi file ghi ra bị xóa sạch
/// sau mỗi lần deploy, restart hoặc spin-down (gói free tự ngủ sau ~15 phút không có request). Kết quả
/// là ảnh upload xong hiện được một lúc rồi 404 vĩnh viễn. Persistent Disk của Render lại chỉ có ở gói
/// trả phí, trong khi Supabase Storage đã nằm sẵn trong hệ thống và free tier 1GB là đủ.
///
/// Trả về URL TUYỆT ĐỐI. Cả admin lẫn mobile đều đã giữ nguyên URL tuyệt đối khi hiển thị
/// (resolveAssetUrl chỉ ghép base URL cho đường dẫn bắt đầu bằng "/"), nên không phải sửa client nào.
/// </summary>
public class SupabaseFileStorageService(
    HttpClient httpClient,
    IOptions<SupabaseStorageSettings> settings,
    ILogger<SupabaseFileStorageService> logger) : IFileStorageService
{
    private readonly SupabaseStorageSettings _settings = settings.Value;

    public async Task<string> SaveAsync(
        Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var baseUrl = _settings.Url.TrimEnd('/');
        var objectPath = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{baseUrl}/storage/v1/object/{_settings.Bucket}/{objectPath}");

        // Service role key, không phải anon key — anon bị RLS của Storage chặn, trả 403.
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ServiceKey);

        var body = new StreamContent(content);
        body.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        request.Content = body;

        var response = await httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "Upload lên Supabase Storage thất bại ({Status}): {Error}", (int)response.StatusCode, error);

            throw new InvalidOperationException(
                $"Không tải được file lên kho lưu trữ ({(int)response.StatusCode}).");
        }

        // Bucket phải để public thì đường dẫn này mới truy cập được mà không cần token.
        return $"{baseUrl}/storage/v1/object/public/{_settings.Bucket}/{objectPath}";
    }
}
