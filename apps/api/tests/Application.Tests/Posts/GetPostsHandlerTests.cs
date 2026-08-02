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

        var result = await handler.Handle(new GetPostsQuery(null, null, null, null), CancellationToken.None);

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

        var result = await handler.Handle(new GetPostsQuery("Tư vấn", null, null, null), CancellationToken.None);

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

        var result = await handler.Handle(new GetPostsQuery(null, "published", null, null), CancellationToken.None);

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

        var result = await handler.Handle(new GetPostsQuery(null, "draft", null, null), CancellationToken.None);

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

        var result = await handler.Handle(new GetPostsQuery(null, "PUBLISHED", null, null), CancellationToken.None);

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

        var result = await handler.Handle(new GetPostsQuery(null, null, "chăm sóc", null), CancellationToken.None);

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

        var result = await handler.Handle(new GetPostsQuery(null, null, "nguyễn", null), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Filter theo serviceId chỉ trả về bài viết gắn đúng dịch vụ đó,
    /// loại bỏ bài viết gắn dịch vụ khác hoặc không gắn dịch vụ nào (ServiceId = null).
    /// </summary>
    [Test]
    public async Task HandleAsync_FilterByServiceId_ReturnsOnlyMatchingServicePosts()
    {
        var serviceId = Guid.NewGuid();
        var post1 = Post.Create("Bài A", "Tư vấn", "BS. Test", "Nội dung", null, false, serviceId);
        var post2 = Post.Create("Bài B", "Tư vấn", "BS. Test", "Nội dung", null, false, Guid.NewGuid());
        var post3 = Post.Create("Bài C", "Tư vấn", "BS. Test", "Nội dung", null, false, null);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post> { post1, post2, post3 });
        var handler = new GetPostsHandler(_repo);

        var result = await handler.Handle(new GetPostsQuery(null, null, null, serviceId), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().ServiceId.Should().Be(serviceId);
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

        var result = await handler.Handle(new GetPostsQuery("Tư vấn", "published", "chăm sóc", null), CancellationToken.None);

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

        var result = await handler.Handle(new GetPostsQuery("KhôngTồnTại", null, null, null), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static Post MakePost(
        string title = "Tiêu đề",
        string category = "Tư vấn",
        string author = "BS. Test",
        bool isPublished = false)
        => Post.Create(title, category, author, "Nội dung", null, isPublished);
}
