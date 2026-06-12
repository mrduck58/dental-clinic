using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Posts;

public class DeletePostHandler(IPostRepository postRepository)
{
    public async Task HandleAsync(Guid id, CancellationToken ct = default)
    {
        var post = await postRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy bài viết với ID: {id}");

        await postRepository.DeleteAsync(post, ct);
    }
}
