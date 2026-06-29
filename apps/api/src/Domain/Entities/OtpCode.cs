using System.Security.Cryptography;

namespace DentalClinic.API.Domain.Entities;

public class OtpCode
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private OtpCode() { }

    public static OtpCode Create(string email, int expiryMinutes = 5)
    {
        return new OtpCode
        {
            Id = Guid.NewGuid(),
            Email = email,
            Code = GenerateCode(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
        };
    }


    public bool IsValid() => !IsUsed && DateTime.UtcNow < ExpiresAt;

    public void MarkUsed() => IsUsed = true;

    private static string GenerateCode()
    {
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        return (BitConverter.ToUInt32(bytes) % 1_000_000).ToString("D6");
    }
}
