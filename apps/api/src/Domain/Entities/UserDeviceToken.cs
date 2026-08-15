namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Lưu trữ FCM Device Token của thiết bị người dùng trong Database để gửi thông báo đẩy native
/// </summary>
public class UserDeviceToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string? DeviceType { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
