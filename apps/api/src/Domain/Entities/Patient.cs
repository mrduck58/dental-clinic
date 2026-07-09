namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Thông tin hồ sơ của bệnh nhân.
/// Có thể được liên kết với tài khoản User nếu bệnh nhân dùng Mobile App.
/// </summary>
public class Patient
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; } // Nullable — bệnh nhân tạo thủ công không cần tài khoản
    public string FullName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public string Gender { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; } // Lưu trực tiếp cho walk-in patient chưa có tài khoản
    public string? Address { get; private set; }
    public string? MedicalHistory { get; private set; } // Tiền sử bệnh lý, dị ứng thuốc...

    // Family relationship fields
    public Guid? PrimaryPatientId { get; private set; }
    public string? Relationship { get; private set; }
    public string? ProfilePictureUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public User? User { get; private set; }
    public Patient? PrimaryPatient { get; private set; }
    public ICollection<Patient> FamilyMembers { get; private set; } = [];
    public ICollection<Appointment> Appointments { get; private set; } = [];
    public ICollection<MedicalRecord> MedicalRecords { get; private set; } = [];

    private Patient() { }

    public static Patient Create(
        string fullName, DateOnly dateOfBirth, string gender, Guid? userId = null,
        string? phoneNumber = null, Guid? primaryPatientId = null, string? relationship = null,
        string? profilePictureUrl = null)
    {
        return new Patient
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            UserId = userId,
            PhoneNumber = phoneNumber,
            PrimaryPatientId = primaryPatientId,
            Relationship = relationship,
            ProfilePictureUrl = profilePictureUrl,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetFullName(string name) => FullName = name;
    public void SetPhoneNumber(string? phone) => PhoneNumber = phone;
    public void SetDateOfBirth(DateOnly dob) => DateOfBirth = dob;
    public void SetGender(string gender) => Gender = gender;
    public void UpdateMedicalHistory(string history) => MedicalHistory = history;
    public void UpdateFamilyRelation(Guid? primaryPatientId, string? relationship)
    {
        PrimaryPatientId = primaryPatientId;
        Relationship = relationship;
    }
    public void UpdateProfilePicture(string? url) => ProfilePictureUrl = url;
}
