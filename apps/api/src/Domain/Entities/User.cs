namespace DentalClinic.API.Domain.Entities;

public class User
{
    public const string DefaultEmploymentStatus = "Active";

    public Guid Id { get; private set; }
    public string? Username { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string? FullName { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }

    // Staff-specific fields
    public string? EmployeeId { get; private set; }
    public string? Department { get; private set; }
    public string? EmploymentStatus { get; private set; } = DefaultEmploymentStatus;
    public string? ProfilePictureUrl { get; private set; }
    public string? ProfessionalNotes { get; private set; }

    // Doctor-specific fields
    public string? Specialty { get; private set; }
    public string? LicenseNumber { get; private set; }
    public int? YearsOfExperience { get; private set; }

    // Extended staff/doctor fields
    public string? Gender { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? Address { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public string? ServicesHandled { get; private set; }
    public DateOnly? CertificateIssuedDate { get; private set; }
    public string? CertificateIssuedBy { get; private set; }
    public string? Education { get; private set; }
    public string? Bio { get; private set; }
    public string? Position { get; private set; }

    // Salary & Leave fields
    public string? EmploymentType { get; private set; }   // "Full-time", "Part-time", "Intern"
    public decimal? BaseSalary { get; private set; }
    public string? SalaryUnit { get; private set; }       // "Theo tháng", "Theo ngày", "Theo ca"
    public decimal? LeaveAccrued { get; private set; }    // Ngày phép tích luỹ/tháng

    public bool HasAccount => PasswordHash != null;

    // Navigation properties
    public Patient? Patient { get; private set; }
    public Dentist? Dentist { get; private set; }

    private User() { }

    /// <summary>Tạo user có tài khoản đăng nhập (dùng cho Admin/tạo tài khoản trực tiếp).</summary>
    public static User Create(
        string username, string email, string passwordHash, string role,
        string? phoneNumber = null, string? fullName = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            PhoneNumber = phoneNumber,
            FullName = fullName,
            EmploymentStatus = DefaultEmploymentStatus,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Tạo hồ sơ nhân viên chưa có tài khoản đăng nhập.</summary>
    public static User CreateEmployee(string email, string role, string? phoneNumber = null, string? fullName = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = null,
            Email = email,
            PasswordHash = null,
            Role = role,
            PhoneNumber = phoneNumber,
            FullName = fullName,
            EmploymentStatus = DefaultEmploymentStatus,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetCredentials(string username, string passwordHash)
    {
        Username = username;
        PasswordHash = passwordHash;
    }

    public void SetStaffProfile(StaffProfileData profile)
    {
        EmployeeId = profile.EmployeeId;
        Department = profile.Department;
        EmploymentStatus = profile.EmploymentStatus ?? DefaultEmploymentStatus;
        ProfilePictureUrl = profile.ProfilePictureUrl;
        ProfessionalNotes = profile.ProfessionalNotes;
        Specialty = profile.Specialty;
        LicenseNumber = profile.LicenseNumber;
        YearsOfExperience = profile.YearsOfExperience;
        Gender = profile.Gender;
        DateOfBirth = profile.DateOfBirth;
        Address = profile.Address;
        StartDate = profile.StartDate;
        ServicesHandled = profile.ServicesHandled;
        CertificateIssuedDate = profile.CertificateIssuedDate;
        CertificateIssuedBy = profile.CertificateIssuedBy;
        Education = profile.Education;
        Bio = profile.Bio;
        Position = profile.Position;
        EmploymentType = profile.EmploymentType;
        BaseSalary = profile.BaseSalary;
        SalaryUnit = profile.SalaryUnit;
        LeaveAccrued = profile.LeaveAccrued;
    }

    public void Update(UpdateStaffData data)
    {
        FullName = data.FullName;
        Email = data.Email;
        PhoneNumber = data.PhoneNumber;
        Role = data.Role;
        Department = data.Department;
        EmploymentStatus = data.EmploymentStatus ?? DefaultEmploymentStatus;
        ProfilePictureUrl = data.ProfilePictureUrl;
        ProfessionalNotes = data.ProfessionalNotes;
        IsActive = data.IsActive;
        Specialty = data.Specialty;
        LicenseNumber = data.LicenseNumber;
        YearsOfExperience = data.YearsOfExperience;
        Gender = data.Gender;
        DateOfBirth = data.DateOfBirth;
        Address = data.Address;
        StartDate = data.StartDate;
        ServicesHandled = data.ServicesHandled;
        CertificateIssuedDate = data.CertificateIssuedDate;
        CertificateIssuedBy = data.CertificateIssuedBy;
        Education = data.Education;
        Bio = data.Bio;
        Position = data.Position;
        EmploymentType = data.EmploymentType;
        BaseSalary = data.BaseSalary;
        SalaryUnit = data.SalaryUnit;
        LeaveAccrued = data.LeaveAccrued;
    }

    public void UpdatePatientProfile(string fullName, string phoneNumber, DateOnly? dateOfBirth, string? gender, string? profilePictureUrl = null)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        if (profilePictureUrl != null)
        {
            ProfilePictureUrl = profilePictureUrl;
        }
    }

    public void UpdatePersonalProfile(
        string fullName,
        string? phoneNumber,
        DateOnly? dateOfBirth,
        string? gender,
        string? address,
        string? profilePictureUrl,
        string? bio,
        string? education,
        string? specialty = null,
        int? yearsOfExperience = null)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Address = address;
        ProfilePictureUrl = profilePictureUrl;
        Bio = bio;
        Education = education;
        if (Role == "Dentist")
        {
            Specialty = specialty ?? Specialty;
            YearsOfExperience = yearsOfExperience ?? YearsOfExperience;
        }
    }

    public void ResetPassword(string newPasswordHash) => PasswordHash = newPasswordHash;

    public void SetActive(bool isActive) => IsActive = isActive;
}

public record StaffProfileData(
    string? EmployeeId,
    string? Department,
    string? EmploymentStatus,
    string? ProfilePictureUrl,
    string? ProfessionalNotes,
    string? Specialty,
    string? LicenseNumber,
    int? YearsOfExperience,
    string? Gender,
    DateOnly? DateOfBirth,
    string? Address,
    DateOnly? StartDate,
    string? ServicesHandled,
    DateOnly? CertificateIssuedDate,
    string? CertificateIssuedBy,
    string? Education,
    string? Bio,
    string? Position,
    string? EmploymentType,
    decimal? BaseSalary,
    string? SalaryUnit,
    decimal? LeaveAccrued);

public record UpdateStaffData(
    string FullName,
    string Email,
    string PhoneNumber,
    string Role,
    string? Department,
    string? EmploymentStatus,
    string? ProfilePictureUrl,
    string? ProfessionalNotes,
    bool IsActive,
    string? Specialty,
    string? LicenseNumber,
    int? YearsOfExperience,
    string? Gender,
    DateOnly? DateOfBirth,
    string? Address,
    DateOnly? StartDate,
    string? ServicesHandled,
    DateOnly? CertificateIssuedDate,
    string? CertificateIssuedBy,
    string? Education,
    string? Bio,
    string? Position,
    string? EmploymentType,
    decimal? BaseSalary,
    string? SalaryUnit,
    decimal? LeaveAccrued);
