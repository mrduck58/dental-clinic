using DentalClinic.API.Application.DTOs.Posts;
using DentalClinic.API.Application.UseCases.Posts;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Posts;

[TestFixture]
public class CreatePostHandlerTests
{
    private IPostRepository _repo = null!;
    private IActivityLogService _activityLog = null!;
    private ICurrentUserService _currentUser = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IPostRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
    }

    /// <summary>
    /// Tạo bài viết hợp lệ phải gọi AddAsync đúng 1 lần để lưu vào database.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_CallsAddAsyncOnce()
    {
        var handler = new CreatePostHandler(_repo, _activityLog, _currentUser);

        await handler.HandleAsync(BuildRequest());

        await _repo.Received(1).AddAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Response trả về phải ánh xạ đúng các trường từ request sang DTO,
    /// đảm bảo caller nhận được thông tin bài viết vừa tạo.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_ReturnsDtoWithCorrectFields()
    {
        var handler = new CreatePostHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(BuildRequest(title: "Tiêu đề", author: "BS. Nam", category: "Tư vấn"));

        result.Title.Should().Be("Tiêu đề");
        result.Author.Should().Be("BS. Nam");
        result.Category.Should().Be("Tư vấn");
    }

    /// <summary>
    /// Khi tạo bài viết ở trạng thái published, PublishedAt phải được gán giá trị,
    /// dùng để hiển thị ngày đăng trên giao diện.
    /// </summary>
    [Test]
    public async Task HandleAsync_IsPublishedTrue_PublishedAtIsSet()
    {
        var handler = new CreatePostHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(BuildRequest(isPublished: true));

        result.IsPublished.Should().BeTrue();
        result.PublishedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Bài viết tạo ở trạng thái draft phải có PublishedAt = null,
    /// tránh hiển thị ngày đăng khi bài chưa được công khai.
    /// </summary>
    [Test]
    public async Task HandleAsync_IsPublishedFalse_PublishedAtIsNull()
    {
        var handler = new CreatePostHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(BuildRequest(isPublished: false));

        result.IsPublished.Should().BeFalse();
        result.PublishedAt.Should().BeNull();
    }

    private static CreatePostRequest BuildRequest(
        string title = "Tiêu đề test",
        string category = "Tư vấn",
        string author = "BS. Test",
        bool isPublished = false)
        => new(title, category, author, "Nội dung", "https://cdn/thumb.jpg", isPublished, null);
}
