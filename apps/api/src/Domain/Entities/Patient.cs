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
    public string? Address { get; private set; }
    public string? MedicalHistory { get; private set; } // Tiền sử bệnh lý, dị ứng thuốc...

    // Navigation properties
    public User? User { get; private set; }
    public ICollection<Appointment> Appointments { get; private set; } = [];
    public ICollection<MedicalRecord> MedicalRecords { get; private set; } = [];

    private Patient() { }

    public static Patient Create(string fullName, DateOnly dateOfBirth, string gender, Guid? userId = null)
    {
        return new Patient
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            UserId = userId
        };
    }

    public void UpdateMedicalHistory(string history) => MedicalHistory = history;
}
