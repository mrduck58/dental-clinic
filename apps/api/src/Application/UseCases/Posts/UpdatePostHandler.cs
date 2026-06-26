using DentalClinic.API.Application.DTOs.Posts;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Posts;

public class UpdatePostHandler(IPostRepository postRepository)
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
        return GetPostsHandler.ToDto(post);
    }
}
