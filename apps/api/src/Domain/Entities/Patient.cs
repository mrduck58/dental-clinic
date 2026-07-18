namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Thông tin hồ sơ của bệnh nhân.
/// Được liên kết với tài khoản User 1-1.
/// </summary>
public class Patient
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? Address { get; private set; }
    public string? ProfilePictureUrl { get; private set; }
    public string? MedicalHistory { get; private set; } // Tiền sử bệnh lý, dị ứng thuốc...

    // Family relationship fields
    public Guid? PrimaryPatientId { get; private set; }
    public string? Relationship { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Read-only delegated properties from User
    public string FullName => User?.FullName ?? string.Empty;
    public string Gender => User?.Gender ?? string.Empty;
    public string? PhoneNumber => User?.PhoneNumber;

    // Navigation properties
    public User User { get; set; } = null!;
    public Patient? PrimaryPatient { get; private set; }
    public ICollection<Patient> FamilyMembers { get; private set; } = [];
    public ICollection<Appointment> Appointments { get; private set; } = [];

    private Patient() { }

    public static Patient Create(
        Guid userId, DateOnly? dateOfBirth = null, string? gender = null,
        string? phoneNumber = null, Guid? primaryPatientId = null, string? relationship = null,
        string? profilePictureUrl = null, string? address = null)
    {
        return new Patient
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DateOfBirth = dateOfBirth,
            Address = address,
            ProfilePictureUrl = profilePictureUrl,
            PrimaryPatientId = primaryPatientId,
            Relationship = relationship,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetFullName(string name)
    {
        if (User != null)
        {
            User.UpdateFullName(name);
        }
    }
    
    public void SetPhoneNumber(string? phone)
    {
        if (User != null)
        {
            User.UpdatePhoneNumber(phone);
        }
    }
    
    public void SetGender(string gender)
    {
        if (User != null)
        {
            User.UpdateGender(gender);
        }
    }

    public void SetDateOfBirth(DateOnly? dob) => DateOfBirth = dob;
    public void UpdateMedicalHistory(string history) => MedicalHistory = history;
    public void UpdateFamilyRelation(Guid? primaryPatientId, string? relationship)
    {
        PrimaryPatientId = primaryPatientId;
        Relationship = relationship;
    }
    public void UpdateProfilePicture(string? url) => ProfilePictureUrl = url;
}
