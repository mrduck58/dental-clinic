using System.Net.Http.Json;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalClinic.API.Infrastructure.Services;

/// <summary>
/// Gửi email qua HTTP API của Brevo.
///
/// Bỏ hẳn SMTP: Render chặn cổng SMTP (25/465/587) nên bản SMTP không bao giờ gửi được trên môi
/// trường thật, mà giữ nó làm dự phòng cũng vô nghĩa vì đó chính là thứ đang hỏng. Brevo đi qua
/// cổng 443 nên chạy được ở cả máy dev lẫn Render, chỉ cần một đường duy nhất.
/// </summary>
public class EmailService(
    HttpClient httpClient,
    IOptions<EmailSettings> options,
    IHostEnvironment environment,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    /// <summary>
    /// Trả về true nếu phải BỎ QUA việc gửi (đang ở Development và chưa cấu hình SMTP).
    /// Ở môi trường thật thì NÉM LỖI thay vì bỏ qua.
    ///
    /// Trước đây mọi môi trường đều âm thầm ghi log rồi return như đã gửi thành công. Hậu quả thật:
    /// deploy lên Render mà thiếu EmailSettings__FromEmail thì API trả 200 "đã gửi mã", không email
    /// nào được gửi, và MÃ OTP LẪN MẬT KHẨU bị ghi thẳng vào log production. Không ai biết hệ thống
    /// đang hỏng cho tới khi người dùng phản ánh.
    /// </summary>
    private bool SkipWhenUnconfigured(string devLogTemplate, params object?[] devLogArgs)
    {
        if (_settings.IsConfigured) return false;

        if (environment.IsDevelopment())
        {
            logger.LogWarning(devLogTemplate, devLogArgs);
            return true;
        }

        throw new InvalidOperationException(
            "Chưa cấu hình EmailSettings (cần SmtpHost và FromEmail) — không gửi được email. "
            + "Trên Render/Vercel phải khai qua biến môi trường EmailSettings__SmtpHost, "
            + "EmailSettings__FromEmail, EmailSettings__Username, EmailSettings__Password.");
    }

    public async Task SendStaffCredentialsAsync(
        string recipientEmail,
        string recipientName,
        string password,
        CancellationToken ct = default)
    {
        if (SkipWhenUnconfigured(
                "[DEV-EMAIL] Tài khoản mới: Email={Email} | Password={Password}", recipientEmail, password))
            return;

        await SendViaBrevoAsync(
            recipientEmail, recipientName,
            $"[{_settings.ClinicName}] Thông tin đăng nhập hệ thống",
            BuildHtml(recipientName, recipientEmail, password),
            BuildPlainText(recipientName, recipientEmail, password), ct);

    }

    public async Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetLink,
        CancellationToken ct = default)
    {
        if (SkipWhenUnconfigured(
                "[DEV-EMAIL] Reset mật khẩu: Email={Email} | Link={Link}", recipientEmail, resetLink))
            return;

        await SendViaBrevoAsync(
            recipientEmail, recipientName,
            $"[{_settings.ClinicName}] Đặt lại mật khẩu của bạn",
            BuildPasswordResetHtml(recipientName, resetLink),
            $"Nhấp vào liên kết sau để đặt lại mật khẩu (hiệu lực 1 giờ):\n{resetLink}\n\nNếu bạn không yêu cầu, hãy bỏ qua email này.", ct);

    }

    public async Task SendPasswordResetOtpAsync(
        string recipientEmail,
        string code,
        CancellationToken ct = default)
    {
        if (SkipWhenUnconfigured(
                "[DEV-EMAIL] OTP đặt lại mật khẩu cho {Email}: {Code}", recipientEmail, code))
            return;

        await SendViaBrevoAsync(
            recipientEmail, recipientEmail,
            $"[{_settings.ClinicName}] Mã đặt lại mật khẩu của bạn",
            BuildPasswordResetOtpHtml(code),
            $"Mã đặt lại mật khẩu của bạn là: {code}\nMã có hiệu lực trong 5 phút. Không chia sẻ mã này cho ai.", ct);

    }

    public async Task SendPatientAccountVerificationAsync(
        string recipientEmail,
        string code,
        CancellationToken ct = default)
    {
        if (SkipWhenUnconfigured(
                "[DEV-EMAIL] Mã xác thực email cho {Email}: {Code}", recipientEmail, code))
            return;

        await SendViaBrevoAsync(
            recipientEmail, recipientEmail,
            $"[{_settings.ClinicName}] Mã xác thực email",
            BuildPatientAccountVerificationHtml(code),
            $"Mã xác thực email của bạn là: {code}\n"
                     + "Đọc mã này cho nhân viên phòng khám để hoàn tất việc tạo tài khoản.\n"
                     + "Mã có hiệu lực trong 5 phút.", ct);

    }

    /// <summary>
    /// Đường gửi DUY NHẤT. Trước đây khối kết nối SMTP bị chép nguyên si ở cả 4 method, nên sửa một
    /// chỗ là phải nhớ sửa ba chỗ còn lại.
    ///
    /// Brevo trả 201 khi nhận thư vào hàng đợi. Lỗi nghiệp vụ (sai key, chưa xác thực địa chỉ gửi,
    /// hết hạn mức ngày) trả 4xx kèm mô tả — ném ra kèm nội dung đó thay vì nuốt, để lỗi cấu hình
    /// lộ ngay chứ không im lặng như bản SMTP cũ.
    /// </summary>
    private async Task SendViaBrevoAsync(
        string toEmail, string toName, string subject, string html, string text, CancellationToken ct)
    {
        var payload = new
        {
            sender = new { name = _settings.FromName, email = _settings.FromEmail },
            to = new[] { new { email = toEmail, name = string.IsNullOrWhiteSpace(toName) ? toEmail : toName } },
            subject,
            htmlContent = html,
            textContent = text,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Add("api-key", _settings.ApiKey);

        var response = await httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Brevo từ chối gửi tới {Email} ({Status}): {Error}",
                toEmail, (int)response.StatusCode, error);

            throw new InvalidOperationException(
                $"Không gửi được email tới {toEmail} ({(int)response.StatusCode}).");
        }

        logger.LogInformation("Đã gửi email tới {Email}: {Subject}", toEmail, subject);
    }

    private string BuildPatientAccountVerificationHtml(string code) => $"""
        <!DOCTYPE html>
        <html lang="vi">
        <head><meta charset="utf-8"/></head>
        <body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;padding:20px;">
          <div style="background:#c0392b;padding:24px 20px;border-radius:10px 10px 0 0;text-align:center;">
            <h1 style="color:#fff;margin:0;font-size:22px;">🦷 {_settings.ClinicName}</h1>
            <p style="color:rgba(255,255,255,.8);margin:6px 0 0;font-size:13px;">Xác thực địa chỉ email</p>
          </div>
          <div style="background:#fff;padding:32px;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 10px 10px;">
            <h2 style="color:#1e293b;margin-top:0;">Mã xác thực email</h2>
            <p style="color:#475569;">Nhân viên phòng khám đang tạo tài khoản cho bạn. Vui lòng <strong>đọc mã bên dưới cho nhân viên</strong> để xác nhận đây đúng là email của bạn. Mã có hiệu lực trong <strong>5 phút</strong>.</p>

            <div style="background:#fef2f2;border:2px solid #fca5a5;border-radius:12px;padding:28px;margin:28px 0;text-align:center;">
              <p style="margin:0 0 8px;color:#64748b;font-size:13px;text-transform:uppercase;letter-spacing:2px;">Mã xác thực</p>
              <p style="margin:0;font-size:48px;font-weight:700;color:#c0392b;letter-spacing:12px;font-family:monospace;">{code}</p>
            </div>

            <div style="background:#fff7ed;border:1px solid #fed7aa;border-radius:8px;padding:16px;margin-bottom:24px;">
              <p style="color:#9a3412;font-size:13px;margin:0;">⚠️ Nếu bạn <strong>không</strong> đang ở phòng khám và không yêu cầu tạo tài khoản, hãy bỏ qua email này và <strong>không đọc mã cho ai</strong>.</p>
            </div>

            <p style="color:#94a3b8;font-size:12px;border-top:1px solid #f1f5f9;padding-top:16px;margin-bottom:0;">
              Email này được gửi tự động — vui lòng không trả lời.<br/>
              <strong>{_settings.ClinicName}</strong>
            </p>
          </div>
        </body>
        </html>
        """;

    private string BuildPasswordResetOtpHtml(string code) => $"""
        <!DOCTYPE html>
        <html lang="vi">
        <head><meta charset="utf-8"/></head>
        <body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;padding:20px;">
          <div style="background:#c0392b;padding:24px 20px;border-radius:10px 10px 0 0;text-align:center;">
            <h1 style="color:#fff;margin:0;font-size:22px;">🦷 {_settings.ClinicName}</h1>
            <p style="color:rgba(255,255,255,.8);margin:6px 0 0;font-size:13px;">Đặt lại mật khẩu</p>
          </div>
          <div style="background:#fff;padding:32px;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 10px 10px;">
            <h2 style="color:#1e293b;margin-top:0;">Mã đặt lại mật khẩu</h2>
            <p style="color:#475569;">Nhập mã bên dưới vào ứng dụng để đặt lại mật khẩu. Mã có hiệu lực trong <strong>5 phút</strong>.</p>

            <div style="background:#fef2f2;border:2px solid #fca5a5;border-radius:12px;padding:28px;margin:28px 0;text-align:center;">
              <p style="margin:0 0 8px;color:#64748b;font-size:13px;text-transform:uppercase;letter-spacing:2px;">Mã OTP</p>
              <p style="margin:0;font-size:48px;font-weight:700;color:#c0392b;letter-spacing:12px;font-family:monospace;">{code}</p>
            </div>

            <div style="background:#fff7ed;border:1px solid #fed7aa;border-radius:8px;padding:16px;margin-bottom:24px;">
              <p style="color:#9a3412;font-size:13px;margin:0;">⚠️ Nếu bạn <strong>không</strong> yêu cầu đặt lại mật khẩu, hãy bỏ qua email này — mật khẩu của bạn vẫn an toàn. Không chia sẻ mã này cho bất kỳ ai.</p>
            </div>

            <p style="color:#94a3b8;font-size:12px;border-top:1px solid #f1f5f9;padding-top:16px;margin-bottom:0;">
              Email này được gửi tự động — vui lòng không trả lời.<br/>
              <strong>{_settings.ClinicName}</strong>
            </p>
          </div>
        </body>
        </html>
        """;

    private string BuildPasswordResetHtml(string name, string resetLink) => $"""
        <!DOCTYPE html>
        <html lang="vi">
        <head><meta charset="utf-8"/></head>
        <body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;padding:20px;">
          <div style="background:#c0392b;padding:24px 20px;border-radius:10px 10px 0 0;text-align:center;">
            <h1 style="color:#fff;margin:0;font-size:22px;">🦷 {_settings.ClinicName}</h1>
            <p style="color:rgba(255,255,255,.8);margin:6px 0 0;font-size:13px;">Hệ thống quản lý nội bộ</p>
          </div>
          <div style="background:#fff;padding:32px;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 10px 10px;">
            <h2 style="color:#1e293b;margin-top:0;">Đặt lại mật khẩu</h2>
            <p style="color:#475569;">Xin chào <strong>{name}</strong>,</p>
            <p style="color:#475569;">Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Nhấp vào nút bên dưới để tiến hành. Liên kết có hiệu lực trong <strong>1 giờ</strong>.</p>

            <div style="text-align:center;margin:32px 0;">
              <a href="{resetLink}"
                 style="background:#c0392b;color:#fff;text-decoration:none;padding:14px 32px;border-radius:8px;font-weight:700;font-size:15px;display:inline-block;">
                Đặt lại mật khẩu
              </a>
            </div>

            <p style="color:#64748b;font-size:13px;">Hoặc dán liên kết sau vào trình duyệt:</p>
            <p style="word-break:break-all;color:#c0392b;font-size:12px;">{resetLink}</p>

            <div style="background:#fff7ed;border:1px solid #fed7aa;border-radius:8px;padding:16px;margin:24px 0;">
              <p style="color:#9a3412;font-size:13px;margin:0;">⚠️ Nếu bạn <strong>không</strong> thực hiện yêu cầu này, hãy bỏ qua email — mật khẩu của bạn vẫn an toàn. Không chia sẻ liên kết này cho bất kỳ ai.</p>
            </div>

            <p style="color:#94a3b8;font-size:12px;border-top:1px solid #f1f5f9;padding-top:16px;margin-bottom:0;">
              Email này được gửi tự động — vui lòng không trả lời.<br/>
              <strong>{_settings.ClinicName}</strong>
            </p>
          </div>
        </body>
        </html>
        """;

    private string BuildHtml(string name, string email, string password) => $"""
        <!DOCTYPE html>
        <html lang="vi">
        <head><meta charset="utf-8"/></head>
        <body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;padding:20px;">
          <div style="background:#c0392b;padding:24px 20px;border-radius:10px 10px 0 0;text-align:center;">
            <h1 style="color:#fff;margin:0;font-size:22px;">🦷 {_settings.ClinicName}</h1>
            <p style="color:rgba(255,255,255,.8);margin:6px 0 0;font-size:13px;">Hệ thống quản lý nội bộ</p>
          </div>
          <div style="background:#fff;padding:32px;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 10px 10px;">
            <h2 style="color:#1e293b;margin-top:0;">Xin chào, {name}!</h2>
            <p style="color:#475569;">Tài khoản đăng nhập hệ thống đã được tạo. Vui lòng lưu thông tin bên dưới và <strong>không chia sẻ cho bất kỳ ai</strong>.</p>

            <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:20px;margin:24px 0;">
              <table style="width:100%;border-collapse:collapse;">
                <tr>
                  <td style="padding:8px 0;color:#64748b;font-size:14px;width:130px;">📧 Email đăng nhập:</td>
                  <td style="padding:8px 0;font-weight:700;font-size:14px;">{email}</td>
                </tr>
                <tr>
                  <td style="padding:8px 0;color:#64748b;font-size:14px;">🔑 Mật khẩu:</td>
                  <td style="padding:8px 0;font-family:monospace;font-size:18px;font-weight:700;color:#c0392b;letter-spacing:3px;">{password}</td>
                </tr>
              </table>
            </div>

            <div style="background:#fff7ed;border:1px solid #fed7aa;border-radius:8px;padding:18px;margin:0 0 24px;">
              <p style="color:#c2410c;font-weight:700;margin:0 0 10px;font-size:15px;">⚠️ CẢNH BÁO BẢO MẬT QUAN TRỌNG</p>
              <ul style="color:#9a3412;font-size:13px;margin:0;padding-left:18px;line-height:2;">
                <li><strong>TUYỆT ĐỐI không tiết lộ mật khẩu</strong> cho bất kỳ ai — kể cả quản trị viên hay đồng nghiệp.</li>
                <li>Vui lòng <strong>đổi mật khẩu ngay</strong> sau lần đăng nhập đầu tiên.</li>
                <li>Không lưu mật khẩu trên thiết bị dùng chung hoặc trình duyệt.</li>
                <li>Nếu nghi ngờ tài khoản bị xâm phạm, <strong>liên hệ quản trị viên ngay lập tức</strong>.</li>
              </ul>
            </div>

            <p style="color:#94a3b8;font-size:12px;border-top:1px solid #f1f5f9;padding-top:16px;margin-bottom:0;">
              Email này được gửi tự động — vui lòng không trả lời.<br/>
              <strong>{_settings.ClinicName}</strong>
            </p>
          </div>
        </body>
        </html>
        """;

    private static string BuildPlainText(string name, string email, string password) => $"""
        Xin chào {name},

        Tài khoản đăng nhập hệ thống đã được tạo:

          Email:    {email}
          Mật khẩu: {password}

        ⚠️  CẢNH BÁO BẢO MẬT:
        - TUYỆT ĐỐI không tiết lộ mật khẩu cho bất kỳ ai.
        - Đổi mật khẩu ngay sau lần đăng nhập đầu tiên.
        - Nếu nghi ngờ tài khoản bị xâm phạm, liên hệ quản trị viên ngay.

        Email này được gửi tự động — vui lòng không trả lời.
        """;
}
