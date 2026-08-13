using DentalClinic.API.Application.DTOs.Posts;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Posts;

public record GetPostByIdQuery(Guid Id) : IRequest<PostDto>;

public class GetPostByIdHandler(IPostRepository postRepository) : IRequestHandler<GetPostByIdQuery, PostDto>
{
    public async Task<PostDto> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
    {
        var post = await postRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy bài viết với ID: {request.Id}");

        return GetPostsHandler.ToDto(post);
    }
}
