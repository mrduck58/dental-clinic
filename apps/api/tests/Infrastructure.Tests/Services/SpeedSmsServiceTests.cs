using System.Net;
using System.Text;
using System.Text.Json;
using DentalClinic.API.Infrastructure.Services;
using DentalClinic.API.Infrastructure.Settings;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Services;

[TestFixture]
public class SpeedSmsServiceTests
{
    /// <summary>Bắt lại request cuối cùng để khẳng định đúng thứ được gửi lên nhà cung cấp.</summary>
    private sealed class CapturingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static IHostEnvironment Env(string name)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName = name;
        return env;
    }

    private static (SpeedSmsService Service, CapturingHandler Handler) Build(
        SmsSettings settings,
        HttpStatusCode status = HttpStatusCode.OK,
        string body = """{"status":"success","code":"00"}""",
        string environmentName = "Production")
    {
        var handler = new CapturingHandler(status, body);
        var client = new HttpClient(handler) { BaseAddress = new Uri(settings.BaseUrl) };

        return (new SpeedSmsService(
            client, Options.Create(settings), Env(environmentName),
            NullLogger<SpeedSmsService>.Instance), handler);
    }

    private static SmsSettings Configured(int smsType = 4, string? sender = null) => new()
    {
        ApiToken = "test-token",
        SmsType = smsType,
        Sender = sender,
    };

    [Test]
    public async Task SendAsync_Configured_PostsToProviderWithBasicAuth()
    {
        var (service, handler) = Build(Configured());

        await service.SendAsync("0912345678", "Ma xac thuc: 123456");

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.AbsolutePath.Should().EndWith("/sms/send");

        // Tài liệu SpeedSMS: token đóng vai username, mật khẩu cố định "x".
        var auth = handler.LastRequest.Headers.Authorization!;
        auth.Scheme.Should().Be("Basic");
        Encoding.UTF8.GetString(Convert.FromBase64String(auth.Parameter!)).Should().Be("test-token:x");
    }

    /// <summary>
    /// Số lưu trong hệ thống là dạng nội địa "09..." — phải đổi sang +84 trước khi gửi.
    /// Đọc giá trị đã parse chứ không so chuỗi thô: System.Text.Json escape "+" thành "+"
    /// (vẫn là JSON hợp lệ, phía nhận decode đúng) nên so chuỗi sẽ trượt oan.
    /// </summary>
    [TestCase("0912345678", "+84912345678")]
    [TestCase("84912345678", "+84912345678")]
    [TestCase("+84912345678", "+84912345678")]
    [TestCase("091 234 5678", "+84912345678")]
    public async Task SendAsync_NormalisesPhoneNumberToE164(string input, string expected)
    {
        var (service, handler) = Build(Configured());

        await service.SendAsync(input, "noi dung");

        using var doc = JsonDocument.Parse(handler.LastBody!);
        doc.RootElement.GetProperty("to")[0].GetString().Should().Be(expected);
    }

    /// <summary>sender chỉ có ý nghĩa với brandname riêng — gửi kèm ở loại khác là thừa.</summary>
    [Test]
    public async Task SendAsync_SharedBrandname_DoesNotSendSender()
    {
        var (service, handler) = Build(Configured(smsType: 4));

        await service.SendAsync("0912345678", "noi dung");

        using var doc = JsonDocument.Parse(handler.LastBody!);
        doc.RootElement.TryGetProperty("sender", out _).Should().BeFalse();
        doc.RootElement.GetProperty("sms_type").GetInt32().Should().Be(4);
    }

    [Test]
    public async Task SendAsync_OwnBrandname_SendsSender()
    {
        var (service, handler) = Build(Configured(smsType: 3, sender: "SONGGIANG"));

        await service.SendAsync("0912345678", "noi dung");

        using var doc = JsonDocument.Parse(handler.LastBody!);
        doc.RootElement.GetProperty("sender").GetString().Should().Be("SONGGIANG");
    }

    /// <summary>Chọn brandname riêng mà quên khai tên định danh thì phải nổ ngay, không gửi hụt.</summary>
    [Test]
    public async Task SendAsync_OwnBrandnameWithoutSender_Throws()
    {
        var (service, _) = Build(Configured(smsType: 3, sender: null));

        Func<Task> act = () => service.SendAsync("0912345678", "noi dung");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Sender*");
    }

    /// <summary>
    /// SpeedSMS trả HTTP 200 kèm status "error" cho lỗi nghiệp vụ (hết tiền, token hỏng, số sai) —
    /// chỉ nhìn mã HTTP sẽ tưởng đã gửi thành công.
    /// </summary>
    [Test]
    public async Task SendAsync_ProviderReturnsErrorInBodyWithHttp200_Throws()
    {
        var (service, _) = Build(Configured(), body: """{"status":"error","code":"105","message":"balance not enough"}""");

        Func<Task> act = () => service.SendAsync("0912345678", "noi dung");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*balance not enough*");
    }

    [Test]
    public async Task SendAsync_HttpError_Throws()
    {
        var (service, _) = Build(Configured(), status: HttpStatusCode.Unauthorized, body: "unauthorized");

        Func<Task> act = () => service.SendAsync("0912345678", "noi dung");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Chưa cấu hình token ở môi trường thật phải NỔ, không im lặng bỏ qua — người dùng ngồi chờ mã
    /// không bao giờ tới mà không ai biết hệ thống đang hỏng.
    /// </summary>
    [Test]
    public async Task SendAsync_NotConfiguredInProduction_Throws()
    {
        var (service, handler) = Build(new SmsSettings { ApiToken = "" });

        Func<Task> act = () => service.SendAsync("0912345678", "noi dung");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*ApiToken*");
        handler.LastRequest.Should().BeNull("không được gọi nhà cung cấp khi chưa cấu hình");
    }

    /// <summary>Ở Development thì chỉ ghi log cho tiện thử, không bắt lập trình viên phải có tài khoản SMS.</summary>
    [Test]
    public async Task SendAsync_NotConfiguredInDevelopment_LogsAndSkips()
    {
        var (service, handler) = Build(new SmsSettings { ApiToken = "" }, environmentName: "Development");

        Func<Task> act = () => service.SendAsync("0912345678", "noi dung");

        await act.Should().NotThrowAsync();
        handler.LastRequest.Should().BeNull();
    }
}
