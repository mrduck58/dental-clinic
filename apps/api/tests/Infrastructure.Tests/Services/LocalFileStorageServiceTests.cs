using DentalClinic.API.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Services;

[TestFixture]
public class LocalFileStorageServiceTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ls_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private LocalFileStorageService CreateSut()
    {
        var env = Substitute.For<IHostEnvironment>();
        var config = Substitute.For<IConfiguration>();
        config["FileStorage:UploadsPath"].Returns(_tempDir);
        return new LocalFileStorageService(env, config);
    }

    /// <summary>
    /// URL trả về phải bắt đầu bằng "/uploads/" — đây là prefix dùng để serve file qua HTTP.
    /// </summary>
    [Test]
    public async Task SaveAsync_ValidStream_ReturnsUploadUrl()
    {
        var sut = CreateSut();
        using var stream = new MemoryStream("test content"u8.ToArray());

        var url = await sut.SaveAsync(stream, "test.png", "image/png");

        url.Should().StartWith("/uploads/");
    }

    /// <summary>
    /// File phải được tạo thực sự trên đĩa sau khi lưu.
    /// </summary>
    [Test]
    public async Task SaveAsync_ValidStream_CreatesFileOnDisk()
    {
        var sut = CreateSut();
        using var stream = new MemoryStream("file content"u8.ToArray());

        var url = await sut.SaveAsync(stream, "photo.jpg", "image/jpeg");
        var fileName = Path.GetFileName(url);
        var filePath = Path.Combine(_tempDir, fileName);

        File.Exists(filePath).Should().BeTrue();
    }

    /// <summary>
    /// Phần mở rộng của file gốc phải được giữ nguyên trong tên file được lưu,
    /// để web server phục vụ đúng Content-Type.
    /// </summary>
    [Test]
    public async Task SaveAsync_PreservesOriginalExtension()
    {
        var sut = CreateSut();
        using var stream = new MemoryStream("content"u8.ToArray());

        var url = await sut.SaveAsync(stream, "avatar.png", "image/png");

        url.Should().EndWith(".png");
    }

    /// <summary>
    /// Hai lần lưu cùng tên file gốc phải trả về URL khác nhau (dùng GUID),
    /// tránh ghi đè file cũ.
    /// </summary>
    [Test]
    public async Task SaveAsync_SameOriginalFileName_ReturnsDifferentUrls()
    {
        var sut = CreateSut();
        using var s1 = new MemoryStream("content1"u8.ToArray());
        using var s2 = new MemoryStream("content2"u8.ToArray());

        var url1 = await sut.SaveAsync(s1, "avatar.jpg", "image/jpeg");
        var url2 = await sut.SaveAsync(s2, "avatar.jpg", "image/jpeg");

        url1.Should().NotBe(url2);
    }
}
