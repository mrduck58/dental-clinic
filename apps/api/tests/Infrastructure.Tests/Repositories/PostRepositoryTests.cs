using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Repositories;

[TestFixture]
public class PostRepositoryTests
{
    private AppDbContext _db = null!;
    private PostRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new PostRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    /// <summary>
    /// GetAllAsync phải trả về tất cả bài viết.
    /// </summary>
    [Test]
    public async Task GetAllAsync_ReturnsAllPosts()
    {
        await _db.Posts.AddRangeAsync(MakePost("Bài 1"), MakePost("Bài 2"), MakePost("Bài 3"));
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(3);
    }

    /// <summary>
    /// GetAllAsync phải trả về bài mới nhất trước (OrderByDescending CreatedAt).
    /// </summary>
    [Test]
    public async Task GetAllAsync_OrdersNewestFirst()
    {
        var older = MakePost("Bài cũ");
        var newer = MakePost("Bài mới");
        // Dùng reflection để đặt CreatedAt khác nhau
        typeof(Post).GetProperty("CreatedAt")!.SetValue(older, DateTimeOffset.UtcNow.AddHours(-2));
        typeof(Post).GetProperty("CreatedAt")!.SetValue(newer, DateTimeOffset.UtcNow);
        await _db.Posts.AddRangeAsync(older, newer);
        await _db.SaveChangesAsync();

        var result = (await _sut.GetAllAsync()).ToList();

        result[0].Id.Should().Be(newer.Id, because: "bài mới nhất phải đứng đầu");
        result[1].Id.Should().Be(older.Id);
    }

    /// <summary>
    /// GetByIdAsync với ID tồn tại phải trả về bài viết tương ứng.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsPost()
    {
        var post = MakePost("Bài viết nha khoa");
        await _db.Posts.AddAsync(post);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(post.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(post.Id);
        result.Title.Should().Be("Bài viết nha khoa");
    }

    /// <summary>
    /// GetByIdAsync với ID không tồn tại phải trả về null.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    /// <summary>
    /// AddAsync phải lưu bài viết vào database.
    /// </summary>
    [Test]
    public async Task AddAsync_PersistsPost()
    {
        var post = MakePost("Bài mới");

        await _sut.AddAsync(post);

        var stored = await _db.Posts.FindAsync(post.Id);
        stored.Should().NotBeNull();
    }

    /// <summary>
    /// DeleteAsync phải xóa bài viết khỏi database.
    /// </summary>
    [Test]
    public async Task DeleteAsync_RemovesPostFromDatabase()
    {
        var post = MakePost("Bài cần xóa");
        await _db.Posts.AddAsync(post);
        await _db.SaveChangesAsync();

        await _sut.DeleteAsync(post);

        var stored = await _db.Posts.FindAsync(post.Id);
        stored.Should().BeNull();
    }

    private static Post MakePost(string title)
        => Post.Create(title, "Tin tức", "BS. Test", "Nội dung bài viết", null, false);
}
