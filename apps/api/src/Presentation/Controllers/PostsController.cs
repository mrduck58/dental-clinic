using DentalClinic.API.Application.DTOs.Posts;
using DentalClinic.API.Application.UseCases.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/posts")]
public class PostsController(
    GetPostsHandler getPosts,
    GetPostByIdHandler getById,
    CreatePostHandler create,
    UpdatePostHandler update,
    DeletePostHandler delete,
    GenerateMarketingContentHandler generateAiDraft) : ControllerBase
{
    /// <summary>GET api/posts — Danh sách bài viết (có filter)</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] Guid? serviceId,
        CancellationToken ct)
    {
        var result = await getPosts.HandleAsync(category, status, search, serviceId, ct);
        return Ok(result);
    }

    /// <summary>GET api/posts/{id} — Chi tiết bài viết</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await getById.HandleAsync(id, ct);
        return Ok(result);
    }

    /// <summary>POST api/posts — Tạo bài viết mới (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request, CancellationToken ct)
    {
        var result = await create.HandleAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>PUT api/posts/{id} — Cập nhật bài viết (Staff)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePostRequest request, CancellationToken ct)
    {
        var result = await update.HandleAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>DELETE api/posts/{id} — Xóa bài viết (Staff)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await delete.HandleAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST api/posts/generate-ai-draft — Soạn nháp bài viết bằng AI từ dịch vụ/ưu đãi có sẵn
    /// (Staff). Chỉ trả về nội dung nháp để nhân viên xem lại — không tự tạo/xuất bản bài viết.</summary>
    [HttpPost("generate-ai-draft")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GenerateAiDraft(
        [FromBody] GenerateMarketingContentRequest request, CancellationToken ct)
    {
        var result = await generateAiDraft.HandleAsync(request, ct);
        return Ok(result);
    }
}
