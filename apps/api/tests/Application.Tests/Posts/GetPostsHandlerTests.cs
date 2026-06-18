using DentalClinic.API.Application.UseCases.Posts;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Posts;

[TestFixture]
public class GetPostsHandlerTests
{
    private IPostRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IPostRepository>();

    /// <summary>
    /// Không truyền filter nào phải trả về toàn bộ danh sách bài viết.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoFilters_ReturnsAllPosts()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(category: "Tư vấn"), MakePost(category: "Khuyến mãi"), MakePost(category: "Tư vấn"),
        });
        var handler = new GetPostsHandler(_repo);

        var result = await handler.HandleAsync(null, null, null);

        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Filter theo category chỉ trả về bài thuộc đúng category đó.
    /// </summary>
    [Test]
    public async Task HandleAsync_FilterByCategory_ReturnsOnlyMatchingCategory()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(category: "Tư vấn"), MakePost(category: "Khuyến mãi"), MakePost(category: "Tư vấn"),
        });
        var handler = new GetPostsHandler(_repo);

        var result = await handler.HandleAsync(category: "Tư vấn", null, null);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Category == "Tư vấn");
    }

    /// <summary>
    /// Filter status="published" chỉ trả về bài đã xuất bản (IsPublished = true).
    /// </summary>
    [Test]
    public async Task HandleAsync_FilterByStatusPublished_ReturnsOnlyPublishedPosts()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(isPublished: true), MakePost(isPublished: false), MakePost(isPublished: true),
        });
        var handler = new GetPostsHandler(_repo);

        var result = await handler.HandleAsync(null, status: "published", null);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.IsPublished);
    }

    /// <summary>
    /// Filter status="draft" chỉ trả về bài chưa xuất bản.
    /// </summary>
    [Test]
    public async Task HandleAsync_FilterByStatusDraft_ReturnsOnlyDraftPosts()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(isPublished: true), MakePost(isPublished: false), MakePost(isPublished: false),
        });
        var handler = new GetPostsHandler(_repo);

        var result = await handler.HandleAsync(null, status: "draft", null);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => !p.IsPublished);
    }

    /// <summary>
    /// Filter status không phân biệt hoa thường — "PUBLISHED" cho kết quả giống "published".
    /// </summary>
    [Test]
    public async Task HandleAsync_FilterByStatusCaseInsensitive_ReturnsMatchingPosts()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(isPublished: true), MakePost(isPublished: false),
        });
        var handler = new GetPostsHandler(_repo);

        var result = await handler.HandleAsync(null, status: "PUBLISHED", null);

        result.Should().HaveCount(1);
        result.Single().IsPublished.Should().BeTrue();
    }

    /// <summary>
    /// Filter search khớp theo tiêu đề bài viết không phân biệt hoa thường.
    /// </summary>
    [Test]
    public async Task HandleAsync_SearchByTitle_ReturnsMatchingPosts()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(title: "Cách chăm sóc răng"),
            MakePost(title: "Giới thiệu dịch vụ"),
            MakePost(title: "Chăm sóc sau nhổ răng"),
        });
        var handler = new GetPostsHandler(_repo);

        var result = await handler.HandleAsync(null, null, search: "chăm sóc");

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Filter search khớp theo tên tác giả, không chỉ tiêu đề.
    /// </summary>
    [Test]
    public async Task HandleAsync_SearchByAuthor_ReturnsMatchingPosts()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(author: "BS. Nguyễn"), MakePost(author: "BS. Trần"), MakePost(author: "BS. Nguyễn"),
        });
        var handler = new GetPostsHandler(_repo);

        var result = await handler.HandleAsync(null, null, search: "nguyễn");

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Kết hợp nhiều filter cùng lúc phải áp dụng tất cả (AND logic).
    /// </summary>
    [Test]
    public async Task HandleAsync_MultipleFilters_AppliesAllFilters()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(title: "Chăm sóc răng", category: "Tư vấn",    isPublished: true),
            MakePost(title: "Chăm sóc răng", category: "Tư vấn",    isPublished: false),
            MakePost(title: "Giới thiệu",    category: "Tư vấn",    isPublished: true),
            MakePost(title: "Chăm sóc răng", category: "Khuyến mãi", isPublished: true),
        });
        var handler = new GetPostsHandler(_repo);

        var result = await handler.HandleAsync(category: "Tư vấn", status: "published", search: "chăm sóc");

        result.Should().HaveCount(1);
    }

    /// <summary>
    /// Không có bài nào khớp filter phải trả về danh sách rỗng, không ném exception.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoMatchingFilter_ReturnsEmpty()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post> { MakePost(category: "Tư vấn") });
        var handler = new GetPostsHandler(_repo);

        var result = await handler.HandleAsync(category: "KhôngTồnTại", null, null);

        result.Should().BeEmpty();
    }

    private static Post MakePost(
        string title = "Tiêu đề",
        string category = "Tư vấn",
        string author = "BS. Test",
        bool isPublished = false)
        => Post.Create(title, category, author, "Nội dung", null, isPublished);
}
