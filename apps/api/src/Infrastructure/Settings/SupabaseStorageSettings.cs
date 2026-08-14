namespace DentalClinic.API.Infrastructure.Settings;

public record SupabaseStorageSettings
{
    /// <summary>URL project Supabase, ví dụ https://xxxx.supabase.co (không có dấu / ở cuối).</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// Service role key — KHÔNG phải anon key. Anon key bị chặn bởi RLS của Storage nên upload sẽ
    /// trả 403. Khóa này có toàn quyền, chỉ nạp qua biến môi trường, tuyệt đối không commit.
    /// </summary>
    public string ServiceKey { get; init; } = string.Empty;

    /// <summary>Tên bucket đã tạo trong Supabase Studio; phải để chế độ public thì ảnh mới hiển thị được.</summary>
    public string Bucket { get; init; } = "uploads";

    /// <summary>
    /// Bỏ trống thì hệ thống tự lùi về lưu đĩa cục bộ — để máy dev chạy được mà không cần khóa thật.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(ServiceKey);
}
