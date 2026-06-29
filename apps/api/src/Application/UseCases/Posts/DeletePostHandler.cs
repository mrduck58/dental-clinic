using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;

namespace DentalClinic.API.Application.UseCases.Posts;

public class DeletePostHandler(
    IPostRepository postRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
{
    public async Task HandleAsync(Guid id, CancellationToken ct = default)
    {
        var post = await postRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy bài viết với ID: {id}");

        await postRepository.DeleteAsync(post, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Delete,
            module: ActivityModule.Post,
            description: $"Xóa bài viết: {post.Title}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: id.ToString(),
            ct: ct);
    }
}
