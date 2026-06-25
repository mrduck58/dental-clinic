using DentalClinic.API.Application.DTOs.Posts;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Posts;

public class CreatePostHandler(IPostRepository postRepository)
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
        return GetPostsHandler.ToDto(post);
    }
}
