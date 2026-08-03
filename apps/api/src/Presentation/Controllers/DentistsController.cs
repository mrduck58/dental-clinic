using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Application.UseCases.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/dentists")]
public class DentistsController(
    GetDentistsHandler getDentistsHandler,
    GetDentistSlotsHandler getDentistSlotsHandler,
    GetDentistDetailHandler getDentistDetailHandler,
    DentistReviewHandler dentistReviewHandler) : ControllerBase
{
    /// <summary>GET api/dentists — Danh sách nha sĩ cho trang chủ mobile</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetDentists(CancellationToken cancellationToken)
    {
        var result = await getDentistsHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/dentists/slots?date=2026-06-20 — Nha sĩ kèm slot khả dụng cho ngày đã chọn</summary>
    [HttpGet("slots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDentistSlots(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var result = await getDentistSlotsHandler.HandleAsync(date, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/dentists/{id}/working-dates?year=2026&month=7 — Các ngày làm việc có slot của nha sĩ trong tháng</summary>
    [HttpGet("{id}/working-dates")]
    [AllowAnonymous]
    public async Task<IActionResult> GetWorkingDates(
        string id,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var dentistGuid))
        {
            return Ok(Enumerable.Empty<string>());
        }
        var dates = await getDentistSlotsHandler.GetWorkingDatesForDentistAsync(dentistGuid, year, month, cancellationToken);
        return Ok(dates);
    }

    /// <summary>GET api/dentists/{id} — Chi tiết hồ sơ nha sĩ (bio, kinh nghiệm, số bệnh nhân đã khám thật)</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDentistDetail(Guid id, CancellationToken cancellationToken)
    {
        var result = await getDentistDetailHandler.HandleAsync(id, cancellationToken);
        if (result is null) return NotFound(new { title = "Không tìm thấy nha sĩ." });
        return Ok(result);
    }

    /// <summary>GET api/dentists/{id}/reviews — Danh sách đánh giá + điểm trung bình của nha sĩ</summary>
    [HttpGet("{id:guid}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDentistReviews(Guid id, CancellationToken cancellationToken)
    {
        var result = await dentistReviewHandler.GetForDentistAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/dentists/{id}/reviews — Gửi (hoặc cập nhật) đánh giá của bệnh nhân hiện tại cho nha sĩ</summary>
    [HttpPost("{id:guid}/reviews")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> CreateDentistReview(
        Guid id, [FromBody] CreateDentistReviewRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await dentistReviewHandler.UpsertAsync(id, userId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/dentists/{id}/review-eligibility — Kiểm tra bệnh nhân có đủ điều kiện đánh giá nha sĩ không và trả về review cũ (nếu có)</summary>
    [HttpGet("{id:guid}/review-eligibility")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetReviewEligibility(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new GetDentistReviewEligibilityQuery(id, userId), cancellationToken);
        return Ok(result);
    }


    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Không xác định được người dùng từ token.");
        return Guid.Parse(sub);
    }
}
