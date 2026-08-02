using DentalClinic.API.Application.DTOs.Posts;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Posts;

public record GetPostsQuery(
    string? Category,
    string? Status,
    string? Search,
    Guid? ServiceId) : IRequest<IEnumerable<PostDto>>;

public class GetPostsHandler(IPostRepository postRepository) : IRequestHandler<GetPostsQuery, IEnumerable<PostDto>>
{
    public async Task<IEnumerable<PostDto>> Handle(GetPostsQuery request, CancellationToken cancellationToken)
    {
        var posts = await postRepository.GetAllAsync(cancellationToken);

        if (request.ServiceId.HasValue)
            posts = posts.Where(p => p.ServiceId == request.ServiceId.Value);

        if (!string.IsNullOrWhiteSpace(request.Category))
            posts = posts.Where(p => p.Category == request.Category);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var isPublished = request.Status.Equals("published", StringComparison.OrdinalIgnoreCase);
            posts = posts.Where(p => p.IsPublished == isPublished);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var q = request.Search.ToLower();
            posts = posts.Where(p =>
                p.Title.ToLower().Contains(q) ||
                p.Author.ToLower().Contains(q));
        }

        return posts.Select(ToDto);
    }

    internal static PostDto ToDto(DentalClinic.API.Domain.Entities.Post p) => new(
        p.Id, p.Title, p.Category, p.Author,
        p.Content, p.ThumbnailUrl, p.IsPublished,
        p.ServiceId, p.Service?.Name,
        p.CreatedAt, p.UpdatedAt, p.PublishedAt);
}
