using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace DentalClinic.API.Presentation.RateLimiting;

/// <summary>
/// Tên policy giới hạn tần suất. Dùng hằng số thay vì gõ chuỗi ở từng action: gõ sai tên policy
/// trong <c>[EnableRateLimiting]</c> làm ứng dụng ném lỗi lúc chạy chứ không phải lúc biên dịch.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Các cổng đăng nhập — chống dò mật khẩu.</summary>
    public const string AuthLogin = "auth-login";

    /// <summary>Nhập mã OTP / token đặt lại mật khẩu — chống dò mã 6 chữ số.</summary>
    public const string AuthOtp = "auth-otp";

    /// <summary>Các endpoint gửi email (đăng ký, gửi lại OTP, quên mật khẩu) — chống spam hòm thư.</summary>
    public const string AuthEmail = "auth-email";
}

public static class AuthRateLimiting
{
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Ngưỡng đặt theo nhịp dùng thật, không theo cảm tính: nhân viên phòng khám đăng nhập
            // vài lần mỗi ngày và cả phòng khám thường đi chung một IP NAT, nên ngưỡng phải chịu
            // được nhiều người cùng lúc mà vẫn đủ chặt để việc dò mã trở nên vô vọng.

            // Đăng nhập: đủ chỗ cho vài người gõ sai mật khẩu rồi thử lại.
            options.AddPolicy(RateLimitPolicies.AuthLogin, PerClientIp(permitLimit: 20, windowMinutes: 5));

            // OTP: chặt nhất. 10 lần/5 phút/IP khiến việc quét không gian 10^6 mã trở nên bất khả thi.
            options.AddPolicy(RateLimitPolicies.AuthOtp, PerClientIp(permitLimit: 10, windowMinutes: 5));

            // Gửi email: mỗi request tốn một email thật gửi đi, siết mạnh nhất về thời gian.
            options.AddPolicy(RateLimitPolicies.AuthEmail, PerClientIp(permitLimit: 5, windowMinutes: 15));

            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                // Nói cho client biết khi nào thử lại được, thay vì để họ quay vòng thử liên tục.
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                var body = JsonSerializer.Serialize(new
                {
                    title = "Bạn đã thao tác quá nhiều lần. Vui lòng thử lại sau ít phút.",
                    status = StatusCodes.Status429TooManyRequests,
                });

                await context.HttpContext.Response.WriteAsync(body, ct);
            };
        });

        return services;
    }

    /// <summary>
    /// Cửa sổ cố định, phân vùng theo IP client. Yêu cầu <c>UseForwardedHeaders</c> đã chạy trước đó,
    /// nếu không mọi request sau nginx sẽ rơi vào chung một phân vùng.
    ///
    /// Request không xác định được IP đi vào phân vùng "unknown" dùng chung — thà để nhóm nặc danh
    /// đó chen nhau trong một hạn mức còn hơn cấp cho mỗi request một hạn mức riêng (tức là bỏ ngỏ).
    /// </summary>
    private static Func<HttpContext, RateLimitPartition<string>> PerClientIp(int permitLimit, int windowMinutes) =>
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(windowMinutes),
                QueueLimit = 0, // xếp hàng chờ ở đây chỉ làm chậm kẻ dò mã chứ không chặn — từ chối luôn.
            });
}
