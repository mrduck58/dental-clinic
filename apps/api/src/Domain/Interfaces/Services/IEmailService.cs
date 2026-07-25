namespace DentalClinic.API.Domain.Interfaces.Services;

public interface IEmailService
{
    Task SendStaffCredentialsAsync(
        string recipientEmail,
        string recipientName,
        string password,
        CancellationToken ct = default);

    Task SendOtpAsync(
        string recipientEmail,
        string code,
        CancellationToken ct = default);

    Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetLink,
        CancellationToken ct = default);

    Task SendPasswordResetOtpAsync(
        string recipientEmail,
        string code,
        CancellationToken ct = default);
}
