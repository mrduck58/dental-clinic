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
