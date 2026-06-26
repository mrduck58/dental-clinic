namespace DentalClinic.API.Domain.Interfaces.Services;

public interface IEmailService
{
    Task SendStaffCredentialsAsync(
        string recipientEmail,
        string recipientName,
        string password,
        CancellationToken ct = default);
}
