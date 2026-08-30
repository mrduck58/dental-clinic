namespace DentalClinic.API.Application.DTOs.Patients;

public record UpdateMedicalHistoryRequest(string? MedicalHistory);

/// <summary><c>HasAccount</c> phân biệt bệnh nhân có tài khoản với bệnh nhân chỉ được tạo tại quầy.</summary>
public record PatientSearchResultDto(
    Guid Id,
    string FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string Gender,
    bool HasAccount,
    string? Relationship = null,
    Guid? PrimaryPatientId = null,
    string? PrimaryPatientName = null
);

public record FamilyMemberDto(
    Guid Id,
    string FullName,
    string Relationship,
    DateOnly? DateOfBirth,
    string Gender,
    string? PhoneNumber,
    string? ProfilePictureUrl
);

public record CreateFamilyMemberRequest(
    string FullName,
    string Relationship,
    DateOnly? DateOfBirth,
    string Gender,
    string? PhoneNumber,
    string? ProfilePictureUrl
);

public record UpdateFamilyMemberRequest(
    string FullName,
    string Relationship,
    DateOnly? DateOfBirth,
    string Gender,
    string? PhoneNumber,
    string? ProfilePictureUrl
);

/// <param name="Email">Bắt buộc — mật khẩu tạm được gửi về địa chỉ này.</param>
/// <param name="PhoneNumber">
/// Bắt buộc — là khóa nhận diện bệnh nhân cũ ở luồng đặt lịch tại quầy. Thiếu nó thì lần sau bệnh
/// nhân quay lại sẽ bị tạo hồ sơ mới và lịch sử khám bị tách đôi.
/// </param>
/// <param name="VerificationCode">Mã bệnh nhân đọc từ hộp thư — bắt buộc, xem RequestPatientEmailVerification.</param>
public record CreatePatientAccountRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string VerificationCode);

public record RequestPatientEmailVerificationRequest(string Email);

/// <summary>Công nợ của một bệnh nhân cho MỘT dịch vụ cụ thể — cộng dồn mọi liệu trình (mọi option/buổi
/// hẹn) của dịch vụ đó. RemainingAmount = TotalCost - AmountPaid, không bao giờ âm.</summary>
public record PatientServiceBalanceDto(
    Guid ServiceId,
    string ServiceName,
    decimal TotalCost,
    decimal AmountPaid,
    decimal RemainingAmount);

/// <summary>Công nợ tổng hợp của một bệnh nhân — đã thanh toán / còn nợ bao nhiêu, theo từng dịch vụ.
/// Công nợ = tổng TotalCost các liệu trình (trừ đã hủy) - số tiền đã thu qua hóa đơn liên quan, cùng
/// công thức đã dùng ở tab "Công nợ" (xem ITreatmentPlanRepository.GetPlanPaidMapAsync).</summary>
public record PatientBalanceDto(
    Guid PatientId,
    string FullName,
    string? PhoneNumber,
    decimal TotalCost,
    decimal AmountPaid,
    decimal RemainingAmount,
    // Số liệu trình (trừ đã hủy) đã từng chỉ định cho bệnh nhân này — 0 nếu chưa từng điều trị.
    int TreatmentPlanCount,
    // Ngày liệu trình gần nhất được lập — null nếu chưa từng điều trị.
    DateTimeOffset? LastTreatmentDate,
    IReadOnlyList<PatientServiceBalanceDto> Services);
