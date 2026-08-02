using DentalClinic.API.Application.DTOs.Posts;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Posts;

public record UpdatePostCommand(
    Guid Id,
    string Title,
    string Category,
    string Content,
    string? ThumbnailUrl,
    bool IsPublished,
    Guid? ServiceId) : IRequest<PostDto>;

public class UpdatePostHandler(
    IPostRepository postRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<UpdatePostCommand, PostDto>
{
    public async Task<PostDto> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        var post = await postRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy bài viết với ID: {request.Id}");

        post.Update(
            request.Title,
            request.Category,
            request.Content,
            request.ThumbnailUrl,
            request.IsPublished,
            request.ServiceId);

        await postRepository.UpdateAsync(post, cancellationToken);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Post,
            description: $"Cập nhật bài viết: {post.Title}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: request.Id.ToString(),
            ct: cancellationToken);

        return GetPostsHandler.ToDto(post);
    }
}
