using DentalClinic.API.Application.DTOs.Posts;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Posts;

public class GetPostsHandler(IPostRepository postRepository)
{
    public async Task<IEnumerable<PostDto>> HandleAsync(
        string? category,
        string? status,
        string? search,
        CancellationToken ct = default)
    {
        var posts = await postRepository.GetAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(category))
            posts = posts.Where(p => p.Category == category);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var isPublished = status.Equals("published", StringComparison.OrdinalIgnoreCase);
            posts = posts.Where(p => p.IsPublished == isPublished);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            posts = posts.Where(p =>
                p.Title.ToLower().Contains(q) ||
                p.Author.ToLower().Contains(q));
        }

        return posts.Select(ToDto);
    }

    internal static PostDto ToDto(DentalClinic.API.Domain.Entities.Post p) => new(
        p.Id, p.Title, p.Category, p.Author,
        p.Content, p.ThumbnailUrl, p.IsPublished,
        p.CreatedAt, p.UpdatedAt, p.PublishedAt);
}
