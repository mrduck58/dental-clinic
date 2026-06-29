using DentalClinic.API.Application.DTOs.Posts;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.Posts;

public class UpdatePostHandler(
    IPostRepository postRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
{
    public async Task<PostDto> HandleAsync(Guid id, UpdatePostRequest request, CancellationToken ct = default)
    {
        var post = await postRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy bài viết với ID: {id}");

        post.Update(
            request.Title,
            request.Category,
            request.Content,
            request.ThumbnailUrl,
            request.IsPublished,
            request.ServiceId);

        await postRepository.UpdateAsync(post, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Post,
            description: $"Cập nhật bài viết: {post.Title}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: id.ToString(),
            ct: ct);

        return GetPostsHandler.ToDto(post);
    }
}
