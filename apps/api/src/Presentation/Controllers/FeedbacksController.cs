using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Application.UseCases.Feedbacks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/feedbacks")]
public class FeedbacksController(
    GetFeedbacksHandler getFeedbacks,
    GetFeedbackByIdHandler getById,
    CreateFeedbackHandler create,
    ApproveFeedbackHandler approve,
    HideFeedbackHandler hide,
    ReplyFeedbackHandler reply) : ControllerBase
{
    /// <summary>GET api/feedbacks — Danh sách phản hồi (Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "Staff,Owner")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var result = await getFeedbacks.HandleAsync(status, search, ct);
        return Ok(result);
    }

    /// <summary>GET api/feedbacks/featured — Đánh giá nổi bật (Public)</summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeatured(CancellationToken ct)
    {
        var result = await getFeedbacks.HandleAsync("Featured", null, ct);
        return Ok(result);
    }

    /// <summary>GET api/feedbacks/{id} — Chi tiết phản hồi (Admin)</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Staff,Owner")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await getById.HandleAsync(id, ct);
        return Ok(result);
    }

    /// <summary>POST api/feedbacks — Khách hàng gửi phản hồi (Public)</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateFeedbackRequest request, CancellationToken ct)
    {
        var result = await create.HandleAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>PUT api/feedbacks/{id}/feature — Đánh dấu nổi bật (Admin)</summary>
    [HttpPut("{id:guid}/feature")]
    [Authorize(Roles = "Staff,Owner")]
    public async Task<IActionResult> Feature(Guid id, CancellationToken ct)
    {
        var result = await approve.HandleAsync(id, ct);
        return Ok(result);
    }

    /// <summary>PUT api/feedbacks/{id}/hide — Ẩn phản hồi (Admin)</summary>
    [HttpPut("{id:guid}/hide")]
    [Authorize(Roles = "Staff,Owner")]
    public async Task<IActionResult> Hide(Guid id, CancellationToken ct)
    {
        var result = await hide.HandleAsync(id, ct);
        return Ok(result);
    }

    /// <summary>POST api/feedbacks/{id}/reply — Trả lời phản hồi (Admin)</summary>
    [HttpPost("{id:guid}/reply")]
    [Authorize(Roles = "Staff,Owner")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyFeedbackRequest request, CancellationToken ct)
    {
        var result = await reply.HandleAsync(id, request, ct);
        return Ok(result);
    }
}
