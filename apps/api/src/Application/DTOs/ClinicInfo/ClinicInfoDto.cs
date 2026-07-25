namespace DentalClinic.API.Application.DTOs.ClinicInfo;

// ── Phần tử trong các danh sách (lưu dạng JSON trong entity) ─────────────────
public record MilestoneDto(int Year, string Description);
public record FeatureDto(string Title, string Description);
public record TreatmentStepDto(string Title, string Description);
public record StatisticDto(string Value, string Label);

/// <summary>DTO trả về cho clinic_website (GET /api/clinic-info).</summary>
public record ClinicInfoDto(
    Guid Id,
    string AboutTitle,
    string AboutDescription,
    int FoundedYear,
    string? AboutImageUrl,
    string Phone,
    string Email,
    string Address,
    string WorkingHours,
    List<MilestoneDto> Milestones,
    List<string> Certifications,
    List<FeatureDto> Features,
    List<TreatmentStepDto> TreatmentSteps,
    List<StatisticDto> Statistics,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

/// <summary>
/// Request cập nhật nội dung (PUT /api/clinic-info — Admin).
/// Các danh sách là tùy chọn: null = giữ nguyên, [] = xóa hết.
/// <see cref="AboutImageUrl"/>: null = giữ nguyên ảnh hiện tại, "" = bỏ ảnh (frontend hiển thị ảnh mặc định).
/// </summary>
public record UpdateClinicInfoRequest(
    string AboutTitle,
    string AboutDescription,
    int FoundedYear,
    string Phone,
    string Email,
    string Address,
    string? AboutImageUrl,
    string? WorkingHours,
    List<MilestoneDto>? Milestones,
    List<string>? Certifications,
    List<FeatureDto>? Features,
    List<TreatmentStepDto>? TreatmentSteps,
    List<StatisticDto>? Statistics);
