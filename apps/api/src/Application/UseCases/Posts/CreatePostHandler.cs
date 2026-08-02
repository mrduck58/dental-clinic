using DentalClinic.API.Application.DTOs.Posts;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Posts;

public record CreatePostCommand(
    string Title,
    string Category,
    string Author,
    string Content,
    string? ThumbnailUrl,
    bool IsPublished,
    Guid? ServiceId) : IRequest<PostDto>;

public class CreatePostHandler(IPostRepository postRepository, IActivityLogService activityLogService, ICurrentUserService currentUser)
    : IRequestHandler<CreatePostCommand, PostDto>
{
    public async Task<PostDto> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var post = Post.Create(
            request.Title,
            request.Category,
            request.Author,
            request.Content,
            request.ThumbnailUrl,
            request.IsPublished,
            request.ServiceId);

        await postRepository.AddAsync(post, cancellationToken);

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
            ct: cancellationToken);

        return GetPostsHandler.ToDto(post);
    }
}
