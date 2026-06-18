using DentalClinic.API.Application.DTOs.Posts;
using DentalClinic.API.Application.UseCases.Posts;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Posts;

[TestFixture]
public class PostHandlerTests
{
    private IPostRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IPostRepository>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CreatePostHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tạo bài viết hợp lệ phải gọi AddAsync đúng 1 lần để lưu vào database.
    /// </summary>
    [Test]
    public async Task Create_ValidRequest_CallsAddAsyncOnce()
    {
        var handler = new CreatePostHandler(_repo);

        await handler.HandleAsync(BuildCreateRequest());

        await _repo.Received(1).AddAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Response trả về phải ánh xạ đúng các trường từ request sang DTO,
    /// đảm bảo caller nhận được thông tin bài viết vừa tạo.
    /// </summary>
    [Test]
    public async Task Create_ValidRequest_ReturnsDtoWithCorrectFields()
    {
        var handler = new CreatePostHandler(_repo);
        var req = BuildCreateRequest(title: "Tiêu đề", author: "BS. Nam", category: "Tư vấn");

        var result = await handler.HandleAsync(req);

        result.Title.Should().Be("Tiêu đề");
        result.Author.Should().Be("BS. Nam");
        result.Category.Should().Be("Tư vấn");
    }

    /// <summary>
    /// Khi tạo bài viết ở trạng thái published, PublishedAt phải được gán giá trị,
    /// dùng để hiển thị ngày đăng trên giao diện.
    /// </summary>
    [Test]
    public async Task Create_IsPublishedTrue_PublishedAtIsSet()
    {
        var handler = new CreatePostHandler(_repo);

        var result = await handler.HandleAsync(BuildCreateRequest(isPublished: true));

        result.IsPublished.Should().BeTrue();
        result.PublishedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Bài viết tạo ở trạng thái draft (chưa xuất bản) phải có PublishedAt = null,
    /// tránh hiển thị ngày đăng khi bài chưa được công khai.
    /// </summary>
    [Test]
    public async Task Create_IsPublishedFalse_PublishedAtIsNull()
    {
        var handler = new CreatePostHandler(_repo);

        var result = await handler.HandleAsync(BuildCreateRequest(isPublished: false));

        result.IsPublished.Should().BeFalse();
        result.PublishedAt.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdatePostHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cập nhật bài viết tồn tại phải gọi UpdateAsync đúng 1 lần và trả về DTO mới.
    /// </summary>
    [Test]
    public async Task Update_ExistingPost_CallsUpdateAsyncOnce()
    {
        var handler = new UpdatePostHandler(_repo);
        var post = MakePost();
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

        await handler.HandleAsync(post.Id, BuildUpdateRequest());

        await _repo.Received(1).UpdateAsync(post, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// DTO trả về sau khi cập nhật phải phản ánh dữ liệu mới từ request,
    /// không phải dữ liệu cũ trước khi update.
    /// </summary>
    [Test]
    public async Task Update_ExistingPost_ReturnsDtoWithNewValues()
    {
        var handler = new UpdatePostHandler(_repo);
        var post = MakePost(title: "Tiêu đề cũ");
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

        var result = await handler.HandleAsync(post.Id, BuildUpdateRequest(title: "Tiêu đề mới"));

        result.Title.Should().Be("Tiêu đề mới");
    }

    /// <summary>
    /// ID bài viết không tồn tại trong database phải ném NotFoundException
    /// để controller trả về HTTP 404, không để lỗi null reference lan ra ngoài.
    /// </summary>
    [Test]
    public async Task Update_PostNotFound_ThrowsNotFoundException()
    {
        var handler = new UpdatePostHandler(_repo);
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Post?)null);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), BuildUpdateRequest());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Truyền ThumbnailUrl = null trong request không được xóa thumbnail hiện có,
    /// cho phép update nội dung mà không cần gửi lại URL ảnh mỗi lần.
    /// </summary>
    [Test]
    public async Task Update_NullThumbnailUrl_DoesNotOverwriteExistingThumbnail()
    {
        var handler = new UpdatePostHandler(_repo);
        var post = MakePost(thumbnailUrl: "https://cdn/old-thumb.jpg");
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

        var result = await handler.HandleAsync(post.Id, BuildUpdateRequest(thumbnailUrl: null));

        result.ThumbnailUrl.Should().Be("https://cdn/old-thumb.jpg");
    }

    /// <summary>
    /// Khi bài viết chuyển từ draft sang published, PublishedAt phải được gán,
    /// đánh dấu đúng thời điểm bài viết được công khai lần đầu.
    /// </summary>
    [Test]
    public async Task Update_DraftToPublished_SetsPublishedAt()
    {
        var handler = new UpdatePostHandler(_repo);
        var post = MakePost(isPublished: false); // draft
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

        var result = await handler.HandleAsync(post.Id, BuildUpdateRequest(isPublished: true));

        result.PublishedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Bài đã published rồi, cập nhật lại với isPublished = true không được thay đổi PublishedAt,
    /// tránh mất thông tin ngày đăng gốc mỗi lần chỉnh sửa nội dung.
    /// </summary>
    [Test]
    public async Task Update_PublishedToPublished_DoesNotChangePublishedAt()
    {
        var handler = new UpdatePostHandler(_repo);
        var post = MakePost(isPublished: true);
        var originalPublishedAt = post.PublishedAt;
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

        var result = await handler.HandleAsync(post.Id, BuildUpdateRequest(isPublished: true));

        result.PublishedAt.Should().Be(originalPublishedAt);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DeletePostHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Xóa bài viết tồn tại phải gọi DeleteAsync đúng 1 lần với đúng entity,
    /// đảm bảo đúng bài viết bị xóa khỏi database.
    /// </summary>
    [Test]
    public async Task Delete_ExistingPost_CallsDeleteAsyncOnce()
    {
        var handler = new DeletePostHandler(_repo);
        var post = MakePost();
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

        await handler.HandleAsync(post.Id);

        await _repo.Received(1).DeleteAsync(post, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Xóa bài viết không tồn tại phải ném NotFoundException,
    /// không được gọi DeleteAsync với entity null gây crash.
    /// </summary>
    [Test]
    public async Task Delete_PostNotFound_ThrowsNotFoundException()
    {
        var handler = new DeletePostHandler(_repo);
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Post?)null);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Khi bài viết không tồn tại, DeleteAsync tuyệt đối không được gọi,
    /// tránh truyền null vào repository gây lỗi không kiểm soát được.
    /// </summary>
    [Test]
    public async Task Delete_PostNotFound_DoesNotCallDeleteAsync()
    {
        var handler = new DeletePostHandler(_repo);
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Post?)null);

        Assert.CatchAsync(() => handler.HandleAsync(Guid.NewGuid()));

        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Post>(), Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetPostsHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Không truyền filter nào phải trả về toàn bộ danh sách bài viết từ repository,
    /// dùng cho trang quản trị cần xem tất cả bài.
    /// </summary>
    [Test]
    public async Task GetPosts_NoFilters_ReturnsAllPosts()
    {
        var handler = new GetPostsHandler(_repo);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(category: "Tư vấn"),
            MakePost(category: "Khuyến mãi"),
            MakePost(category: "Tư vấn"),
        });

        var result = await handler.HandleAsync(null, null, null);

        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Filter theo category chỉ được trả về bài thuộc đúng category đó,
    /// các bài thuộc category khác phải bị loại ra khỏi kết quả.
    /// </summary>
    [Test]
    public async Task GetPosts_FilterByCategory_ReturnsOnlyMatchingCategory()
    {
        var handler = new GetPostsHandler(_repo);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(category: "Tư vấn"),
            MakePost(category: "Khuyến mãi"),
            MakePost(category: "Tư vấn"),
        });

        var result = await handler.HandleAsync(category: "Tư vấn", null, null);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Category == "Tư vấn");
    }

    /// <summary>
    /// Filter status = "published" chỉ trả về bài đã xuất bản (IsPublished = true),
    /// dùng cho trang public chỉ hiển thị bài đã được duyệt.
    /// </summary>
    [Test]
    public async Task GetPosts_FilterByStatusPublished_ReturnsOnlyPublishedPosts()
    {
        var handler = new GetPostsHandler(_repo);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(isPublished: true),
            MakePost(isPublished: false),
            MakePost(isPublished: true),
        });

        var result = await handler.HandleAsync(null, status: "published", null);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.IsPublished);
    }

    /// <summary>
    /// Filter status = "draft" chỉ trả về bài chưa xuất bản (IsPublished = false),
    /// dùng cho trang quản trị xem bài đang chờ duyệt.
    /// </summary>
    [Test]
    public async Task GetPosts_FilterByStatusDraft_ReturnsOnlyDraftPosts()
    {
        var handler = new GetPostsHandler(_repo);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(isPublished: true),
            MakePost(isPublished: false),
            MakePost(isPublished: false),
        });

        var result = await handler.HandleAsync(null, status: "draft", null);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => !p.IsPublished);
    }

    /// <summary>
    /// Filter status không phân biệt hoa thường — "PUBLISHED" phải cho kết quả giống "published",
    /// tránh lỗi do client gửi sai casing mà vẫn trả về kết quả đúng.
    /// </summary>
    [Test]
    public async Task GetPosts_FilterByStatusCaseInsensitive_ReturnsMatchingPosts()
    {
        var handler = new GetPostsHandler(_repo);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(isPublished: true),
            MakePost(isPublished: false),
        });

        var result = await handler.HandleAsync(null, status: "PUBLISHED", null);

        result.Should().HaveCount(1);
        result.Single().IsPublished.Should().BeTrue();
    }

    /// <summary>
    /// Filter search khớp theo tiêu đề bài viết (không phân biệt hoa thường),
    /// dùng cho thanh tìm kiếm trả về bài có tiêu đề chứa từ khóa.
    /// </summary>
    [Test]
    public async Task GetPosts_SearchByTitle_ReturnsMatchingPosts()
    {
        var handler = new GetPostsHandler(_repo);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(title: "Cách chăm sóc răng"),
            MakePost(title: "Giới thiệu dịch vụ"),
            MakePost(title: "Chăm sóc sau nhổ răng"),
        });

        var result = await handler.HandleAsync(null, null, search: "chăm sóc");

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Filter search khớp theo tên tác giả, không chỉ tiêu đề,
    /// cho phép tìm tất cả bài của một bác sĩ cụ thể.
    /// </summary>
    [Test]
    public async Task GetPosts_SearchByAuthor_ReturnsMatchingPosts()
    {
        var handler = new GetPostsHandler(_repo);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(author: "BS. Nguyễn"),
            MakePost(author: "BS. Trần"),
            MakePost(author: "BS. Nguyễn"),
        });

        var result = await handler.HandleAsync(null, null, search: "nguyễn");

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Kết hợp nhiều filter cùng lúc phải áp dụng tất cả đồng thời (AND logic),
    /// chỉ trả về bài thỏa mãn toàn bộ điều kiện.
    /// </summary>
    [Test]
    public async Task GetPosts_MultipleFilters_AppliesAllFilters()
    {
        var handler = new GetPostsHandler(_repo);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(title: "Chăm sóc răng", category: "Tư vấn",    isPublished: true),
            MakePost(title: "Chăm sóc răng", category: "Tư vấn",    isPublished: false),
            MakePost(title: "Giới thiệu",    category: "Tư vấn",    isPublished: true),
            MakePost(title: "Chăm sóc răng", category: "Khuyến mãi", isPublished: true),
        });

        var result = await handler.HandleAsync(
            category: "Tư vấn",
            status: "published",
            search: "chăm sóc");

        result.Should().HaveCount(1);
    }

    /// <summary>
    /// Không có bài nào khớp filter phải trả về danh sách rỗng, không được ném exception.
    /// </summary>
    [Test]
    public async Task GetPosts_NoMatchingFilter_ReturnsEmpty()
    {
        var handler = new GetPostsHandler(_repo);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Post>
        {
            MakePost(category: "Tư vấn"),
        });

        var result = await handler.HandleAsync(category: "KhôngTồnTại", null, null);

        result.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetPostByIdHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy bài viết theo ID tồn tại phải trả về đúng DTO của bài đó,
    /// với đầy đủ thông tin để hiển thị trang chi tiết.
    /// </summary>
    [Test]
    public async Task GetById_ExistingPost_ReturnsDto()
    {
        var handler = new GetPostByIdHandler(_repo);
        var post = MakePost(title: "Bài viết A");
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

        var result = await handler.HandleAsync(post.Id);

        result.Id.Should().Be(post.Id);
        result.Title.Should().Be("Bài viết A");
    }

    /// <summary>
    /// ID không tồn tại phải ném NotFoundException để controller trả về HTTP 404,
    /// không để giá trị null lan ra ngoài gây NullReferenceException.
    /// </summary>
    [Test]
    public async Task GetById_PostNotFound_ThrowsNotFoundException()
    {
        var handler = new GetPostByIdHandler(_repo);
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Post?)null);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static Post MakePost(
        string title = "Tiêu đề mặc định",
        string category = "Tư vấn",
        string author = "BS. Mặc định",
        string? thumbnailUrl = "https://cdn/thumb.jpg",
        bool isPublished = false)
        => Post.Create(title, category, author, "Nội dung bài viết", thumbnailUrl, isPublished);

    private static CreatePostRequest BuildCreateRequest(
        string title = "Tiêu đề test",
        string category = "Tư vấn",
        string author = "BS. Test",
        bool isPublished = false)
        => new(title, category, author, "Nội dung", "https://cdn/thumb.jpg", isPublished);

    private static UpdatePostRequest BuildUpdateRequest(
        string title = "Tiêu đề đã cập nhật",
        string category = "Tư vấn",
        string? thumbnailUrl = "https://cdn/new-thumb.jpg",
        bool isPublished = false)
        => new(title, category, "Nội dung mới", thumbnailUrl, isPublished);
}
