namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Đại diện cho tài khoản người dùng trong hệ thống.
/// Một User có thể có vai trò: Admin, Dentist, Receptionist, hoặc Patient.
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty; // Dùng UserRole enum
    public string? FullName { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation property
    public Patient? Patient { get; private set; }
    public Dentist? Dentist { get; private set; }

    private User() { } // EF Core requires parameterless constructor

    public static User Create(string username, string email, string passwordHash, string role, string? phoneNumber = null, string? fullName = null)
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
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}
