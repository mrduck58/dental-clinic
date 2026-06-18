using DentalClinic.API.Application.DTOs.Posts;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Posts;

public class GetPostByIdHandler(IPostRepository postRepository)
{
    public async Task<PostDto> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var post = await postRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy bài viết với ID: {id}");

        return GetPostsHandler.ToDto(post);
    }
}
