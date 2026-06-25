namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Thông tin giới thiệu phòng khám hiển thị trên trang chủ / trang giới thiệu (clinic_website).
/// Đây là bảng singleton (chỉ có 1 dòng) — gom toàn bộ nội dung tĩnh trước đây hardcode ở frontend:
/// giới thiệu, mốc lịch sử, chứng chỉ, lý do chọn, thống kê, quy trình điều trị, thông tin liên hệ.
///
/// Các danh sách (milestones, certifications, features, steps, statistics) được lưu dưới dạng
/// chuỗi JSON để giữ schema gọn — chỉ 1 bảng, không phát sinh bảng con. Việc (de)serialize JSON
/// được thực hiện ở tầng Application (handler) nên Domain không phụ thuộc kiểu DTO nào.
/// </summary>
public class ClinicInfo
{
    public Guid Id { get; private set; }

    // ── Giới thiệu (About) ──────────────────────────────────────────────────
    public string AboutTitle { get; private set; } = string.Empty;
    /// <summary>Mô tả giới thiệu; các đoạn văn cách nhau bằng "\n\n".</summary>
    public string AboutDescription { get; private set; } = string.Empty;
    public int FoundedYear { get; private set; }
    public string? AboutImageUrl { get; private set; }

    // ── Thông tin liên hệ (Contact) ─────────────────────────────────────────
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;

    // ── Danh sách lưu dạng JSON ──────────────────────────────────────────────
    /// <summary>JSON: [{ "year": 2009, "description": "..." }]</summary>
    public string MilestonesJson { get; private set; } = "[]";
    /// <summary>JSON: ["ISO 9001:2015", "Invisalign Provider"]</summary>
    public string CertificationsJson { get; private set; } = "[]";
    /// <summary>JSON: [{ "title": "...", "description": "..." }]</summary>
    public string FeaturesJson { get; private set; } = "[]";
    /// <summary>JSON: [{ "title": "...", "description": "..." }]</summary>
    public string TreatmentStepsJson { get; private set; } = "[]";
    /// <summary>JSON: [{ "value": "10.000+", "label": "Khách hàng hài lòng" }]</summary>
    public string StatisticsJson { get; private set; } = "[]";

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private ClinicInfo() { }

    public static ClinicInfo Create(
        string aboutTitle,
        string aboutDescription,
        int foundedYear,
        string phone,
        string email,
        string address,
        string? aboutImageUrl = null)
        => new()
        {
            Id = Guid.NewGuid(),
            AboutTitle = aboutTitle,
            AboutDescription = aboutDescription,
            FoundedYear = foundedYear,
            Phone = phone,
            Email = email,
            Address = address,
            AboutImageUrl = aboutImageUrl,
            MilestonesJson = "[]",
            CertificationsJson = "[]",
            FeaturesJson = "[]",
            TreatmentStepsJson = "[]",
            StatisticsJson = "[]",
            CreatedAt = DateTimeOffset.UtcNow,
        };

    /// <summary>
    /// Cập nhật các trường văn bản (giới thiệu + liên hệ).
    /// Quy ước nhất quán với các danh sách: <paramref name="aboutImageUrl"/> = null nghĩa là
    /// GIỮ NGUYÊN ảnh hiện tại (tránh xoá nhầm). Để bỏ ảnh, gửi chuỗi rỗng "" —
    /// frontend coi giá trị rỗng là "không có ảnh" và hiển thị ảnh mặc định.
    /// </summary>
    public void UpdateContent(
        string aboutTitle,
        string aboutDescription,
        int foundedYear,
        string phone,
        string email,
        string address,
        string? aboutImageUrl)
    {
        AboutTitle = aboutTitle;
        AboutDescription = aboutDescription;
        FoundedYear = foundedYear;
        Phone = phone;
        Email = email;
        Address = address;
        if (aboutImageUrl is not null) AboutImageUrl = aboutImageUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Cập nhật các danh sách (đã được tầng Application serialize sẵn thành JSON).</summary>
    public void SetCollections(
        string? milestonesJson,
        string? certificationsJson,
        string? featuresJson,
        string? treatmentStepsJson,
        string? statisticsJson)
    {
        if (milestonesJson is not null) MilestonesJson = milestonesJson;
        if (certificationsJson is not null) CertificationsJson = certificationsJson;
        if (featuresJson is not null) FeaturesJson = featuresJson;
        if (treatmentStepsJson is not null) TreatmentStepsJson = treatmentStepsJson;
        if (statisticsJson is not null) StatisticsJson = statisticsJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
