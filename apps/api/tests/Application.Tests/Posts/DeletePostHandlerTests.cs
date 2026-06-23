using DentalClinic.API.Application.UseCases.Posts;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Posts;

[TestFixture]
public class DeletePostHandlerTests
{
    private IPostRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IPostRepository>();

    /// <summary>
    /// Xóa bài viết tồn tại phải gọi DeleteAsync đúng 1 lần với đúng entity.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingPost_CallsDeleteAsyncOnce()
    {
        var post = MakePost();
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        var handler = new DeletePostHandler(_repo);

        await handler.HandleAsync(post.Id);

        await _repo.Received(1).DeleteAsync(post, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// ID không tồn tại phải ném NotFoundException, không gọi DeleteAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_PostNotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Post?)null);
        var handler = new DeletePostHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Khi bài viết không tồn tại, DeleteAsync tuyệt đối không được gọi,
    /// tránh truyền null vào repository gây lỗi không kiểm soát được.
    /// </summary>
    [Test]
    public async Task HandleAsync_PostNotFound_DoesNotCallDeleteAsync()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Post?)null);
        var handler = new DeletePostHandler(_repo);

        Assert.CatchAsync(() => handler.HandleAsync(Guid.NewGuid()));

        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
    }

    private static Post MakePost()
        => Post.Create("Tiêu đề", "Tư vấn", "BS. Test", "Nội dung", null, false);
}
