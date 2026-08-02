using DentalClinic.API.Application.DTOs.Posts;
using DentalClinic.API.Application.UseCases.Posts;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Posts;

[TestFixture]
public class UpdatePostHandlerTests
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
    /// Cập nhật bài viết tồn tại phải gọi UpdateAsync đúng 1 lần.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingPost_CallsUpdateAsyncOnce()
    {
        var post = MakePost();
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        var handler = new UpdatePostHandler(_repo, _activityLog, _currentUser);

        await handler.Handle(BuildRequest(post.Id), CancellationToken.None);

        await _repo.Received(1).UpdateAsync(post, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// DTO trả về sau khi cập nhật phải phản ánh dữ liệu mới từ request.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingPost_ReturnsDtoWithNewValues()
    {
        var post = MakePost(title: "Tiêu đề cũ");
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        var handler = new UpdatePostHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(BuildRequest(post.Id, title: "Tiêu đề mới"), CancellationToken.None);

        result.Title.Should().Be("Tiêu đề mới");
    }

    /// <summary>
    /// ID không tồn tại phải ném NotFoundException để controller trả về HTTP 404.
    /// </summary>
    [Test]
    public async Task HandleAsync_PostNotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Post?)null);
        var handler = new UpdatePostHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(BuildRequest(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Truyền ThumbnailUrl = null không được xóa thumbnail hiện có,
    /// cho phép update nội dung mà không cần gửi lại URL ảnh mỗi lần.
    /// </summary>
    [Test]
    public async Task HandleAsync_NullThumbnailUrl_DoesNotOverwriteExistingThumbnail()
    {
        var post = MakePost(thumbnailUrl: "https://cdn/old-thumb.jpg");
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        var handler = new UpdatePostHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(BuildRequest(post.Id, thumbnailUrl: null), CancellationToken.None);

        result.ThumbnailUrl.Should().Be("https://cdn/old-thumb.jpg");
    }

    /// <summary>
    /// Khi bài viết chuyển từ draft sang published, PublishedAt phải được gán,
    /// đánh dấu đúng thời điểm bài viết được công khai lần đầu.
    /// </summary>
    [Test]
    public async Task HandleAsync_DraftToPublished_SetsPublishedAt()
    {
        var post = MakePost(isPublished: false);
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        var handler = new UpdatePostHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(BuildRequest(post.Id, isPublished: true), CancellationToken.None);

        result.PublishedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Bài đã published, update lại với isPublished=true không được thay đổi PublishedAt,
    /// tránh mất thông tin ngày đăng gốc mỗi lần chỉnh sửa nội dung.
    /// </summary>
    [Test]
    public async Task HandleAsync_PublishedToPublished_DoesNotChangePublishedAt()
    {
        var post = MakePost(isPublished: true);
        var originalPublishedAt = post.PublishedAt;
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        var handler = new UpdatePostHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(BuildRequest(post.Id, isPublished: true), CancellationToken.None);

        result.PublishedAt.Should().Be(originalPublishedAt);
    }

    /// <summary>
    /// Bài đã published, update lại với isPublished=false (chuyển về draft) không được xóa
    /// PublishedAt gốc — handler chỉ set PublishedAt mới khi published lần đầu, không có logic
    /// xóa khi unpublish, nên giá trị cũ phải được giữ nguyên.
    /// </summary>
    [Test]
    public async Task HandleAsync_PublishedToDraft_DoesNotClearPublishedAt()
    {
        var post = MakePost(isPublished: true);
        var originalPublishedAt = post.PublishedAt;
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        var handler = new UpdatePostHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(BuildRequest(post.Id, isPublished: false), CancellationToken.None);

        result.IsPublished.Should().BeFalse();
        result.PublishedAt.Should().Be(originalPublishedAt);
    }

    /// <summary>
    /// Update luôn ghi đè ServiceId theo request (không giữ giá trị cũ khi null như ThumbnailUrl) —
    /// bài viết đang gắn dịch vụ, update với ServiceId = null phải gỡ liên kết dịch vụ đó.
    /// </summary>
    [Test]
    public async Task HandleAsync_NullServiceId_OverwritesExistingServiceId()
    {
        var post = Post.Create("Tiêu đề", "Tư vấn", "BS. Test", "Nội dung", null, false, Guid.NewGuid());
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        var handler = new UpdatePostHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(BuildRequest(post.Id), CancellationToken.None);

        result.ServiceId.Should().BeNull();
    }

    private static Post MakePost(
        string title = "Tiêu đề mặc định",
        string? thumbnailUrl = "https://cdn/thumb.jpg",
        bool isPublished = false)
        => Post.Create(title, "Tư vấn", "BS. Mặc định", "Nội dung", thumbnailUrl, isPublished);

    private static UpdatePostCommand BuildRequest(
        Guid id,
        string title = "Tiêu đề đã cập nhật",
        string? thumbnailUrl = "https://cdn/new-thumb.jpg",
        bool isPublished = false)
        => new(id, title, "Tư vấn", "Nội dung mới", thumbnailUrl, isPublished, null);
}
