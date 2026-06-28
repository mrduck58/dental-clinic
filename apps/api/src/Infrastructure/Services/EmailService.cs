using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DentalClinic.API.Infrastructure.Services;

public class EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    public async Task SendStaffCredentialsAsync(
        string recipientEmail,
        string recipientName,
        string password,
        CancellationToken ct = default)
    {
        if (!_settings.IsConfigured)
        {
            // Fallback khi chưa cấu hình SMTP — chỉ dùng trong môi trường dev
            logger.LogWarning(
                "[DEV-EMAIL] Tài khoản mới: Email={Email} | Password={Password}",
                recipientEmail, password);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(new MailboxAddress(recipientName, recipientEmail));
        message.Subject = $"[{_settings.ClinicName}] Thông tin đăng nhập hệ thống";

        message.Body = new BodyBuilder
        {
            HtmlBody = BuildHtml(recipientName, recipientEmail, password),
            TextBody = BuildPlainText(recipientName, recipientEmail, password),
        }.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(
            _settings.SmtpHost,
            _settings.SmtpPort,
            _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
            ct);

        if (!string.IsNullOrWhiteSpace(_settings.Username))
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password, ct);

        await smtp.SendAsync(message, ct);
        await smtp.DisconnectAsync(quit: true, ct);
    }

    public async Task SendOtpAsync(
        string recipientEmail,
        string code,
        CancellationToken ct = default)
    {
        if (!_settings.IsConfigured)
        {
            logger.LogWarning("[DEV-EMAIL] OTP cho {Email}: {Code}", recipientEmail, code);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(new MailboxAddress(recipientEmail, recipientEmail));
        message.Subject = $"[{_settings.ClinicName}] Mã xác thực OTP của bạn";

        message.Body = new BodyBuilder
        {
            HtmlBody = BuildOtpHtml(code),
            TextBody = $"Mã OTP của bạn là: {code}\nMã có hiệu lực trong 5 phút. Không chia sẻ mã này cho ai.",
        }.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(
            _settings.SmtpHost,
            _settings.SmtpPort,
            _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
            ct);

        if (!string.IsNullOrWhiteSpace(_settings.Username))
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password, ct);

        await smtp.SendAsync(message, ct);
        await smtp.DisconnectAsync(quit: true, ct);
    }

    public async Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetLink,
        CancellationToken ct = default)
    {
        if (!_settings.IsConfigured)
        {
            logger.LogWarning("[DEV-EMAIL] Reset mật khẩu: Email={Email} | Link={Link}", recipientEmail, resetLink);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(new MailboxAddress(recipientName, recipientEmail));
        message.Subject = $"[{_settings.ClinicName}] Đặt lại mật khẩu của bạn";

        message.Body = new BodyBuilder
        {
            HtmlBody = BuildPasswordResetHtml(recipientName, resetLink),
            TextBody = $"Nhấp vào liên kết sau để đặt lại mật khẩu (hiệu lực 1 giờ):\n{resetLink}\n\nNếu bạn không yêu cầu, hãy bỏ qua email này.",
        }.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(
            _settings.SmtpHost,
            _settings.SmtpPort,
            _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
            ct);

        if (!string.IsNullOrWhiteSpace(_settings.Username))
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password, ct);

        await smtp.SendAsync(message, ct);
        await smtp.DisconnectAsync(quit: true, ct);
    }

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

    private string BuildOtpHtml(string code) => $"""
        <!DOCTYPE html>
        <html lang="vi">
        <head><meta charset="utf-8"/></head>
        <body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;padding:20px;">
          <div style="background:#c0392b;padding:24px 20px;border-radius:10px 10px 0 0;text-align:center;">
            <h1 style="color:#fff;margin:0;font-size:22px;">🦷 {_settings.ClinicName}</h1>
            <p style="color:rgba(255,255,255,.8);margin:6px 0 0;font-size:13px;">Xác thực tài khoản</p>
          </div>
          <div style="background:#fff;padding:32px;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 10px 10px;">
            <h2 style="color:#1e293b;margin-top:0;">Mã xác thực của bạn</h2>
            <p style="color:#475569;">Nhập mã bên dưới vào ứng dụng để hoàn tất đăng ký. Mã có hiệu lực trong <strong>5 phút</strong>.</p>

            <div style="background:#fef2f2;border:2px solid #fca5a5;border-radius:12px;padding:28px;margin:28px 0;text-align:center;">
              <p style="margin:0 0 8px;color:#64748b;font-size:13px;text-transform:uppercase;letter-spacing:2px;">Mã OTP</p>
              <p style="margin:0;font-size:48px;font-weight:700;color:#c0392b;letter-spacing:12px;font-family:monospace;">{code}</p>
            </div>

            <div style="background:#fff7ed;border:1px solid #fed7aa;border-radius:8px;padding:16px;margin-bottom:24px;">
              <p style="color:#9a3412;font-size:13px;margin:0;">⚠️ <strong>Không chia sẻ mã này cho bất kỳ ai</strong> — kể cả nhân viên phòng khám. Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email.</p>
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
