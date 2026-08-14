using DentalClinic.API.Infrastructure.Services;
using DentalClinic.API.Infrastructure.Settings;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Services;

[TestFixture]
public class EmailServiceTests
{
    private ILogger<EmailService> _logger = null!;
    // EmailService nay chi bo qua viec gui khi dang o Development; ngoai Development thi nem loi.
    private IHostEnvironment _environment = null!;
    private HttpClient _httpClient = null!;
    private BrevoHandler _handler = null!;

    /// <summary>Bắt lại request gửi tới Brevo để khẳng định đúng thứ được gửi đi.</summary>
    private sealed class BrevoHandler : HttpMessageHandler
    {
        public System.Net.HttpStatusCode Status { get; set; } = System.Net.HttpStatusCode.Created;
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(Status) { Content = new StringContent("{}") };
        }
    }

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<EmailService>>();
        _environment = Substitute.For<IHostEnvironment>();
        _environment.EnvironmentName = "Development";
        _handler = new BrevoHandler();
        _httpClient = new HttpClient(_handler);
    }

    private EmailService CreateSut(bool smtpConfigured = false)
    {
        var settings = new EmailSettings
        {
            ApiKey = smtpConfigured ? "xkeysib-test" : string.Empty,
            FromEmail = smtpConfigured ? "noreply@clinic.com" : string.Empty,
            FromName = "Dental Clinic",
            ClinicName = "Sông Giang Dental",
        };
        return new EmailService(_httpClient, Options.Create(settings), _environment, _logger);
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

    // ── Đường gửi qua Brevo (thay cho SMTP) ───────────────────────────────────

    /// <summary>
    /// Gửi qua HTTP API của Brevo chứ không phải SMTP: Render chặn cổng SMTP 25/465/587 nên bản
    /// SMTP không bao giờ gửi được ở môi trường thật. Brevo dùng cổng 443.
    /// </summary>
    [Test]
    public async Task SendAsync_Configured_PostsToBrevoWithApiKeyHeader()
    {
        var sut = CreateSut(smtpConfigured: true);

        await sut.SendPasswordResetOtpAsync("benhnhan@test.com", "123456");

        _handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        _handler.LastRequest.RequestUri!.ToString().Should().Be("https://api.brevo.com/v3/smtp/email");
        _handler.LastRequest.Headers.GetValues("api-key").Should().ContainSingle().Which.Should().Be("xkeysib-test");
    }

    [Test]
    public async Task SendAsync_Configured_SendsSenderRecipientAndContent()
    {
        var sut = CreateSut(smtpConfigured: true);

        await sut.SendPasswordResetOtpAsync("benhnhan@test.com", "123456");

        using var doc = System.Text.Json.JsonDocument.Parse(_handler.LastBody!);
        var root = doc.RootElement;
        root.GetProperty("sender").GetProperty("email").GetString().Should().Be("noreply@clinic.com");
        root.GetProperty("to")[0].GetProperty("email").GetString().Should().Be("benhnhan@test.com");
        root.GetProperty("htmlContent").GetString().Should().Contain("123456");
        root.GetProperty("textContent").GetString().Should().Contain("123456");
    }

    /// <summary>
    /// Brevo từ chối (sai key, chưa xác thực địa chỉ gửi, hết hạn mức ngày) thì phải NỔ — nuốt lỗi
    /// chính là cách bản SMTP cũ khiến production im lặng không gửi mà không ai biết.
    /// </summary>
    [Test]
    public async Task SendAsync_BrevoRejects_Throws()
    {
        _handler.Status = System.Net.HttpStatusCode.Unauthorized;
        var sut = CreateSut(smtpConfigured: true);

        Func<Task> act = () => sut.SendPasswordResetOtpAsync("benhnhan@test.com", "123456");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>Ngoài Development mà chưa cấu hình thì phải nổ, không im lặng bỏ qua.</summary>
    [Test]
    public async Task SendAsync_NotConfiguredOutsideDevelopment_Throws()
    {
        _environment.EnvironmentName = "Production";
        var sut = CreateSut(smtpConfigured: false);

        Func<Task> act = () => sut.SendPasswordResetOtpAsync("benhnhan@test.com", "123456");

        await act.Should().ThrowAsync<InvalidOperationException>();
        _handler.LastRequest.Should().BeNull("chua cau hinh thi khong duoc goi Brevo");
    }
}
