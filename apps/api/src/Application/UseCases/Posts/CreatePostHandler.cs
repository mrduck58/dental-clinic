using DentalClinic.API.Application.DTOs.Posts;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;

namespace DentalClinic.API.Application.UseCases.Posts;

public class CreatePostHandler(IPostRepository postRepository, IActivityLogService activityLogService, ICurrentUserService currentUser)
{
    public async Task<PostDto> HandleAsync(CreatePostRequest request, CancellationToken ct = default)
    {
        var post = Post.Create(
            request.Title,
            request.Category,
            request.Author,
            request.Content,
            request.ThumbnailUrl,
            request.IsPublished,
            request.ServiceId);

        await postRepository.AddAsync(post, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Post,
            description: $"Tạo bài viết mới: {request.Title}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: post.Id.ToString(),
            ct: ct);

        return GetPostsHandler.ToDto(post);
    }
}
