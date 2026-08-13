using DentalClinic.API.Application.UseCases.Posts;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Posts;

[TestFixture]
public class GetPostByIdHandlerTests
{
    private IPostRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IPostRepository>();

    /// <summary>
    /// Lấy bài viết theo ID tồn tại phải trả về đúng DTO với đầy đủ thông tin.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingPost_ReturnsDto()
    {
        var post = Post.Create("Bài viết A", "Tư vấn", "BS. Test", "Nội dung", null, false);
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        var handler = new GetPostByIdHandler(_repo);

        var result = await handler.Handle(new GetPostByIdQuery(post.Id), CancellationToken.None);

        result.Id.Should().Be(post.Id);
        result.Title.Should().Be("Bài viết A");
    }

    /// <summary>
    /// ID không tồn tại phải ném NotFoundException, không để null lan ra ngoài.
    /// </summary>
    [Test]
    public async Task HandleAsync_PostNotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Post?)null);
        var handler = new GetPostByIdHandler(_repo);

        Func<Task> act = () => handler.Handle(new GetPostByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
