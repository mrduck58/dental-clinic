namespace DentalClinic.API.Application.DTOs.Patients;

public record UpdateMedicalHistoryRequest(string? MedicalHistory);

/// <summary><c>HasAccount</c> phân biệt bệnh nhân có tài khoản với bệnh nhân chỉ được tạo tại quầy.</summary>
public record PatientSearchResultDto(
    Guid Id,
    string FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string Gender,
    bool HasAccount
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
