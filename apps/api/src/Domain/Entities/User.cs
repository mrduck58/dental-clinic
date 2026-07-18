namespace DentalClinic.API.Domain.Entities;

public class User
{
    public const string DefaultEmploymentStatus = "Active";

    public Guid Id { get; private set; }
    public string? Username { get; private set; }
    public string? Email { get; private set; }
    public string? PasswordHash { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? Gender { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }

    // Password reset
    public string? PasswordResetToken { get; private set; }
    public DateTimeOffset? PasswordResetTokenExpiry { get; private set; }

    // Nhà cung cấp đăng nhập bên ngoài (null = tài khoản local, "Google" = đăng nhập qua Google)
    public string? Provider { get; private set; }

    public bool HasAccount => PasswordHash != null;

    // Navigation properties
    public Patient? Patient { get; private set; }
    public Dentist? Dentist { get; private set; }
    public Staff? Staff { get; private set; }

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
            FullName = fullName ?? string.Empty,
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
            FullName = fullName ?? string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Tạo tài khoản bệnh nhân từ đăng nhập Google — không có mật khẩu, email đã được Google xác thực.</summary>
    public static User CreateGoogleUser(string email, string? fullName, string? profilePictureUrl)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = null,
            Email = email,
            PasswordHash = null,
            Role = "Patient",
            FullName = fullName ?? string.Empty,
            Provider = "Google",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        user.Patient = Patient.Create(user.Id, profilePictureUrl: profilePictureUrl);
        return user;
    }

    public void SetCredentials(string username, string passwordHash)
    {
        Username = username;
        PasswordHash = passwordHash;
    }

    public void UpdateFullName(string name) => FullName = name;
    public void UpdatePhoneNumber(string? phone) => PhoneNumber = phone;
    public void UpdateGender(string? gender) => Gender = gender;

    public void SetStaffProfile(StaffProfileData profile)
    {
        Gender = profile.Gender;

        if (Role == "Staff")
        {
            Staff = Staff.Create(
                Id,
                profile.EmployeeId ?? $"ST-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                profile.Department,
                profile.Position,
                profile.EmploymentStatus ?? DefaultEmploymentStatus,
                profile.EmploymentType,
                profile.StartDate,
                profile.DateOfBirth,
                profile.Address,
                profile.ProfilePictureUrl,
                profile.BaseSalary,
                profile.SalaryUnit,
                profile.Allowance,
                profile.LeaveAccrued
            );
        }
        else if (Role == "Dentist" || Role == "Doctor")
        {
            Dentist = Dentist.Create(
                Id,
                profile.EmployeeId ?? $"DT-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                profile.Specialty ?? "Nha khoa tổng quát",
                profile.LicenseNumber ?? "N/A",
                profile.YearsOfExperience,
                profile.Department,
                profile.Position,
                profile.EmploymentStatus ?? DefaultEmploymentStatus,
                profile.EmploymentType,
                profile.StartDate,
                profile.DateOfBirth,
                profile.Address,
                profile.ProfilePictureUrl,
                profile.BaseSalary,
                profile.SalaryUnit,
                profile.Allowance,
                profile.LeaveAccrued,
                profile.Education,
                profile.Bio,
                profile.CertificateIssuedDate,
                profile.CertificateIssuedBy,
                "morning"
            );
        }
    }

    public void Update(UpdateStaffData data)
    {
        FullName = data.FullName;
        Email = data.Email;
        PhoneNumber = data.PhoneNumber;
        Role = data.Role;
        IsActive = data.IsActive;
        Gender = data.Gender;

        if (Role == "Staff")
        {
            if (Staff == null)
            {
                Staff = Staff.Create(
                    Id,
                    data.EmployeeId ?? $"ST-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    data.Department,
                    data.Position,
                    data.EmploymentStatus ?? DefaultEmploymentStatus,
                    data.EmploymentType,
                    data.StartDate,
                    data.DateOfBirth,
                    data.Address,
                    data.ProfilePictureUrl,
                    data.BaseSalary,
                    data.SalaryUnit,
                    data.Allowance,
                    data.LeaveAccrued
                );
            }
            else
            {
                Staff.Update(
                    data.Department,
                    data.Position,
                    data.EmploymentStatus ?? DefaultEmploymentStatus,
                    data.EmploymentType,
                    data.StartDate,
                    data.DateOfBirth,
                    data.Address,
                    data.ProfilePictureUrl,
                    data.BaseSalary,
                    data.SalaryUnit,
                    data.Allowance,
                    data.LeaveAccrued
                );
            }
        }
        else if (Role == "Dentist" || Role == "Doctor")
        {
            if (Dentist == null)
            {
                Dentist = Dentist.Create(
                    Id,
                    data.EmployeeId ?? $"DT-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    data.Specialty ?? "Nha khoa tổng quát",
                    data.LicenseNumber ?? "N/A",
                    data.YearsOfExperience,
                    data.Department,
                    data.Position,
                    data.EmploymentStatus ?? DefaultEmploymentStatus,
                    data.EmploymentType,
                    data.StartDate,
                    data.DateOfBirth,
                    data.Address,
                    data.ProfilePictureUrl,
                    data.BaseSalary,
                    data.SalaryUnit,
                    data.Allowance,
                    data.LeaveAccrued,
                    data.Education,
                    data.Bio,
                    data.CertificateIssuedDate,
                    data.CertificateIssuedBy,
                    "morning"
                );
            }
            else
            {
                Dentist.Update(
                    data.Specialty ?? "Nha khoa tổng quát",
                    data.LicenseNumber ?? "N/A",
                    data.YearsOfExperience,
                    data.Department,
                    data.Position,
                    data.EmploymentStatus ?? DefaultEmploymentStatus,
                    data.EmploymentType,
                    data.StartDate,
                    data.DateOfBirth,
                    data.Address,
                    data.ProfilePictureUrl,
                    data.BaseSalary,
                    data.SalaryUnit,
                    data.Allowance,
                    data.LeaveAccrued,
                    data.Education,
                    data.Bio,
                    data.CertificateIssuedDate,
                    data.CertificateIssuedBy,
                    Dentist.Shift
                );
            }
        }
    }

    public void UpdatePatientProfile(string fullName, string phoneNumber, DateOnly? dateOfBirth, string? gender, string? profilePictureUrl = null)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
        Gender = gender;
        
        if (Patient == null)
        {
            Patient = Patient.Create(Id, dateOfBirth, profilePictureUrl: profilePictureUrl);
        }
        else
        {
            Patient.SetDateOfBirth(dateOfBirth);
            if (profilePictureUrl != null)
            {
                Patient.UpdateProfilePicture(profilePictureUrl);
            }
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
        Gender = gender;

        if (Role == "Dentist" || Role == "Doctor")
        {
            if (Dentist == null)
            {
                Dentist = Dentist.Create(
                    Id,
                    $"DT-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    specialty ?? "Nha khoa tổng quát",
                    "N/A",
                    yearsOfExperience,
                    null,
                    null,
                    DefaultEmploymentStatus,
                    null,
                    null,
                    dateOfBirth,
                    address,
                    profilePictureUrl,
                    null,
                    null,
                    null,
                    null,
                    education,
                    bio
                );
            }
            else
            {
                Dentist.Update(
                    specialty ?? Dentist.Specialization,
                    Dentist.LicenseNumber,
                    yearsOfExperience ?? Dentist.ExperienceYears,
                    Dentist.Department,
                    Dentist.Position,
                    Dentist.EmploymentStatus,
                    Dentist.EmploymentType,
                    Dentist.StartDate,
                    dateOfBirth ?? Dentist.DateOfBirth,
                    address ?? Dentist.Address,
                    profilePictureUrl ?? Dentist.ProfilePictureUrl,
                    Dentist.BaseSalary,
                    Dentist.SalaryUnit,
                    Dentist.Allowance,
                    Dentist.LeaveAccrued,
                    education ?? Dentist.Education,
                    bio ?? Dentist.Biography,
                    Dentist.CertificateIssuedDate,
                    Dentist.CertificateIssuedBy,
                    Dentist.Shift
                );
            }
        }
        else if (Role == "Staff")
        {
            if (Staff == null)
            {
                Staff = Staff.Create(
                    Id,
                    $"ST-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    null,
                    null,
                    DefaultEmploymentStatus,
                    null,
                    null,
                    dateOfBirth,
                    address,
                    profilePictureUrl
                );
            }
            else
            {
                Staff.Update(
                    Staff.Department,
                    Staff.Position,
                    Staff.EmploymentStatus,
                    Staff.EmploymentType,
                    Staff.StartDate,
                    dateOfBirth ?? Staff.DateOfBirth,
                    address ?? Staff.Address,
                    profilePictureUrl ?? Staff.ProfilePictureUrl,
                    Staff.BaseSalary,
                    Staff.SalaryUnit,
                    Staff.Allowance,
                    Staff.LeaveAccrued
                );
            }
        }
    }

    public void ResetPassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
    }

    public void SetPasswordResetToken(string token, DateTimeOffset expiry)
    {
        PasswordResetToken = token;
        PasswordResetTokenExpiry = expiry;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
    }

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
    decimal? LeaveAccrued,
    decimal? Allowance);

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
    decimal? LeaveAccrued,
    decimal? Allowance,
    string? EmployeeId = null);
