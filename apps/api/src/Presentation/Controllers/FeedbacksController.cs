using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Application.UseCases.Feedbacks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/feedbacks")]
public class FeedbacksController(ISender sender) : ControllerBase
{
    /// <summary>GET api/feedbacks — Danh sách phản hồi (Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "Staff,Owner")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetFeedbacksQuery(status, search), ct);
        return Ok(result);
    }

    /// <summary>GET api/feedbacks/featured — Đánh giá nổi bật (Public)</summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeatured(CancellationToken ct)
    {
        var result = await sender.Send(new GetFeedbacksQuery("Featured", null), ct);
        return Ok(result);
    }

    /// <summary>GET api/feedbacks/{id} — Chi tiết phản hồi (Admin)</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Staff,Owner")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetFeedbackByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>POST api/feedbacks — Khách hàng gửi phản hồi (Public)</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateFeedbackRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateFeedbackCommand(request.CustomerName, request.Rating, request.Comment), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>PUT api/feedbacks/{id}/feature — Đánh dấu nổi bật (Admin)</summary>
    [HttpPut("{id:guid}/feature")]
    [Authorize(Roles = "Staff,Owner")]
    public async Task<IActionResult> Feature(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ApproveFeedbackCommand(id), ct);
        return Ok(result);
    }

    /// <summary>PUT api/feedbacks/{id}/hide — Ẩn phản hồi (Admin)</summary>
    [HttpPut("{id:guid}/hide")]
    [Authorize(Roles = "Staff,Owner")]
    public async Task<IActionResult> Hide(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new HideFeedbackCommand(id), ct);
        return Ok(result);
    }

    /// <summary>POST api/feedbacks/{id}/reply — Trả lời phản hồi (Admin)</summary>
    [HttpPost("{id:guid}/reply")]
    [Authorize(Roles = "Staff,Owner")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyFeedbackRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ReplyFeedbackCommand(id, request.ReplyText), ct);
        return Ok(result);
    }

    /// <summary>POST api/feedbacks/{id}/ai-draft-reply — Soạn nháp câu trả lời bằng AI (Staff/Owner).
    /// Chỉ trả về nội dung nháp để nhân viên xem lại — không tự gửi phản hồi.</summary>
    [HttpPost("{id:guid}/ai-draft-reply")]
    [Authorize(Roles = "Staff,Owner")]
    public async Task<IActionResult> GenerateAiReply(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GenerateFeedbackReplyQuery(id), ct);
        return Ok(result);
    }
}
