namespace DentalClinic.API.Domain.Interfaces.Services;

public record GoogleUserInfo(string Email, string? FullName, string? PictureUrl);

public interface IGoogleAuthService
{
    /// <summary>Xác thực chữ ký/audience/hạn sử dụng của Google ID token, trả về thông tin người dùng đã xác thực.</summary>
    Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken, CancellationToken ct = default);
}
