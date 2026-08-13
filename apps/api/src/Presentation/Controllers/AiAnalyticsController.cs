using DentalClinic.API.Application.UseCases.AiAnalytics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/ai-analytics")]
[Authorize(Roles = "Admin")]
public class AiAnalyticsController(ISender sender) : ControllerBase
{
    /// <summary>GET api/ai-analytics?rangeDays=14 — Thống kê vận hành các tính năng AI (chatbot, tóm tắt
    /// bệnh án, soạn nội dung marketing): số lượng gọi, tỷ lệ lỗi, thời gian phản hồi, hiệu quả chatbot.
    /// Bỏ trống hoặc không truyền <c>rangeDays</c> để lấy TẤT CẢ dữ liệu từ trước tới nay.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] int? rangeDays = 14, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAiAnalyticsQuery(rangeDays), cancellationToken);
        return Ok(result);
    }
}
