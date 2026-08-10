using System.Security.Cryptography;
using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

public class OtpCode
{
    /// <summary>
    /// Số lần nhập sai trước khi mã bị vô hiệu. Mã 6 chữ số mà cho thử không giới hạn thì chỉ là
    /// một khóa 20 bit không có chốt — dò hết là chuyện của thời gian, không phải của may rủi.
    /// </summary>
    public const int MaxAttempts = 5;

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public OtpPurpose Purpose { get; private set; } = OtpPurpose.PasswordReset;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>Số lần đã nhập sai mã này.</summary>
    public int AttemptCount { get; private set; }

    public Guid? UserId { get; private set; }
    public User? User { get; private set; }

    private OtpCode() { }

    public static OtpCode Create(string email, OtpPurpose purpose, int expiryMinutes = 5)
    {
        return new OtpCode
        {
            Id = Guid.NewGuid(),
            Email = email,
            Code = GenerateCode(),
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            IsUsed = false,
            AttemptCount = 0,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public bool IsValid() => !IsUsed && AttemptCount < MaxAttempts && DateTime.UtcNow < ExpiresAt;

    public void MarkUsed() => IsUsed = true;

    /// <summary>
    /// Ghi nhận một lần nhập sai. Đủ <see cref="MaxAttempts"/> lần thì mã coi như đã dùng — người
    /// dùng phải yêu cầu gửi lại mã mới, thay vì tiếp tục dò trên cùng một mã.
    /// </summary>
    public void RegisterFailedAttempt()
    {
        AttemptCount++;

        if (AttemptCount >= MaxAttempts) IsUsed = true;
    }

    private static string GenerateCode()
    {
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        return (BitConverter.ToUInt32(bytes) % 1_000_000).ToString("D6");
    }
}
