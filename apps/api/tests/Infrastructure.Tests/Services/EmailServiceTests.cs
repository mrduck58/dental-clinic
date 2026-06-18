using DentalClinic.API.Infrastructure.Services;
using DentalClinic.API.Infrastructure.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Services;

[TestFixture]
public class EmailServiceTests
{
    private ILogger<EmailService> _logger = null!;

    [SetUp]
    public void SetUp() => _logger = Substitute.For<ILogger<EmailService>>();

    private EmailService CreateSut(bool smtpConfigured = false)
    {
        var settings = new EmailSettings
        {
            SmtpHost = smtpConfigured ? "smtp.example.com" : string.Empty,
            SmtpPort = 587,
            FromEmail = smtpConfigured ? "noreply@clinic.com" : string.Empty,
            FromName = "Dental Clinic",
            ClinicName = "Sông Giang Dental",
        };
        return new EmailService(Options.Create(settings), _logger);
    }

    /// <summary>
    /// Khi SMTP chưa cấu hình (môi trường dev), method không ném exception —
    /// EmailSettings.IsConfigured = false nên chỉ log và return sớm.
    /// </summary>
    [Test]
    public async Task SendStaffCredentialsAsync_SmtpNotConfigured_DoesNotThrow()
    {
        var sut = CreateSut(smtpConfigured: false);

        Func<Task> act = () => sut.SendStaffCredentialsAsync("staff@clinic.com", "Nguyễn Văn A", "SecurePass@123");

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Khi SMTP chưa cấu hình, phải log cảnh báo với email và password để dev có thể xem.
    /// </summary>
    [Test]
    public async Task SendStaffCredentialsAsync_SmtpNotConfigured_LogsWarning()
    {
        var sut = CreateSut(smtpConfigured: false);

        await sut.SendStaffCredentialsAsync("staff@clinic.com", "Nguyễn Văn A", "SecurePass@123");

        _logger.ReceivedWithAnyArgs(1).Log<object>(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
