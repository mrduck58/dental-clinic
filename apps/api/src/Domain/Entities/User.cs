using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Định danh + thông tin đăng nhập. KHÔNG chứa logic dựng Employee/DentistProfile — việc đó thuộc
/// về tầng Application (CreateStaffHandler/UpdateStaffHandler...), User chỉ là identity thuần.
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public string? Username { get; private set; }
    public string? Email { get; private set; }
    public string? PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
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
    public Employee? Employee { get; private set; }

    private User() { }

    /// <summary>Tạo user có tài khoản đăng nhập (dùng cho Admin/tạo tài khoản trực tiếp).</summary>
    public static User Create(
        string username, string email, string passwordHash, UserRole role,
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
    public static User CreateEmployee(string email, UserRole role, string? phoneNumber = null, string? fullName = null)
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
            Role = UserRole.Patient,
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

    /// <summary>Gắn hồ sơ Employee cho User này (được gọi từ CreateStaffHandler sau khi đã tạo Employee).</summary>
    public void AttachEmployee(Employee employee) => Employee = employee;

    /// <summary>Cập nhật thông tin định danh cơ bản (không đụng tới Employee/DentistProfile — do
    /// UpdateStaffHandler tự cập nhật Employee/DentistProfile riêng qua repository tương ứng).</summary>
    public void Update(string fullName, string email, string? phoneNumber, UserRole role, bool isActive, string? gender)
    {
        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        Role = role;
        IsActive = isActive;
        Gender = gender;
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

    /// <summary>Cập nhật thông tin định danh cơ bản khi tự sửa hồ sơ cá nhân (self-service). Với
    /// Dentist/Staff, phần field mở rộng (address/bio/education/specialty...) do FillProfileHandler
    /// tự cập nhật riêng qua Employee/DentistProfile repository sau khi gọi method này.</summary>
    public void UpdatePersonalProfile(string fullName, string? phoneNumber, string? gender)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
        Gender = gender;
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

    // ── Khóa tạm sau nhiều lần đăng nhập sai ──────────────────────────────────
    //
    // Tách hẳn khỏi IsActive: IsActive là quyết định của quản trị viên (vô hiệu hóa tài khoản),
    // LockoutEndAt là biện pháp tự động và tự hết hạn. Gộp chung thì việc dò mật khẩu của kẻ lạ
    // sẽ trông y hệt việc admin cố ý khóa, và mở khóa tự động sẽ vô tình bật lại tài khoản đã bị cấm.
    //
    // Khóa CÓ THỜI HẠN chứ không vĩnh viễn: khóa vĩnh viễn biến chính cơ chế này thành công cụ để
    // kẻ xấu chặn người dùng thật (chỉ cần gõ sai mật khẩu người khác vài lần). Hết hạn thì tự mở.

    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockoutEndAt { get; private set; }

    public bool IsLockedOut(DateTimeOffset now) => LockoutEndAt is { } until && until > now;

    /// <summary>
    /// Ghi nhận một lần đăng nhập sai. Đủ <paramref name="maxAttempts"/> lần thì khóa trong
    /// <paramref name="lockoutDuration"/> và đặt lại bộ đếm, để sau khi hết khóa người dùng lại có
    /// đủ số lần thử chứ không bị khóa lại ngay ở lần sai kế tiếp.
    /// </summary>
    public void RegisterFailedLogin(DateTimeOffset now, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= maxAttempts)
        {
            LockoutEndAt = now.Add(lockoutDuration);
            FailedLoginAttempts = 0;
        }
    }

    /// <summary>Đăng nhập thành công — xóa sạch dấu vết những lần sai trước đó.</summary>
    public void ClearFailedLogins()
    {
        FailedLoginAttempts = 0;
        LockoutEndAt = null;
    }
}
