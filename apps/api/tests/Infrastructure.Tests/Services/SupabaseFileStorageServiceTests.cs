using System.Net;
using System.Text;
using DentalClinic.API.Infrastructure.Services;
using DentalClinic.API.Infrastructure.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Services;

[TestFixture]
public class SupabaseFileStorageServiceTests
{
    private sealed class CapturingHandler(HttpStatusCode status, string body = "{}") : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private static readonly SupabaseStorageSettings Settings = new()
    {
        Url = "https://proj.supabase.co",
        ServiceKey = "service-role-key",
        Bucket = "uploads",
    };

    private static (SupabaseFileStorageService Service, CapturingHandler Handler) Build(
        HttpStatusCode status = HttpStatusCode.OK, SupabaseStorageSettings? settings = null)
    {
        var handler = new CapturingHandler(status);
        return (new SupabaseFileStorageService(
            new HttpClient(handler), Options.Create(settings ?? Settings),
            NullLogger<SupabaseFileStorageService>.Instance), handler);
    }

    private static MemoryStream Content() => new(Encoding.UTF8.GetBytes("fake-image-bytes"));

    [Test]
    public async Task SaveAsync_PostsToBucketPathWithServiceKey()
    {
        var (service, handler) = Build();

        await service.SaveAsync(Content(), "anh.png", "image/png");

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString()
            .Should().StartWith("https://proj.supabase.co/storage/v1/object/uploads/");

        // Phải là service role key — anon key bị RLS của Storage chặn và trả 403.
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("service-role-key");
    }

    /// <summary>
    /// Trả URL TUYỆT ĐỐI để cả admin lẫn mobile hiển thị được mà không phải sửa gì — resolveAssetUrl
    /// của hai client chỉ ghép base URL cho đường dẫn bắt đầu bằng "/", còn URL tuyệt đối giữ nguyên.
    /// </summary>
    [Test]
    public async Task SaveAsync_ReturnsAbsolutePublicUrl()
    {
        var (service, _) = Build();

        var url = await service.SaveAsync(Content(), "anh.png", "image/png");

        url.Should().StartWith("https://proj.supabase.co/storage/v1/object/public/uploads/");
        url.Should().EndWith(".png");
    }

    /// <summary>Tên file do người dùng đặt không được dùng lại — tránh đoán được đường dẫn và ghi đè nhau.</summary>
    [Test]
    public async Task SaveAsync_GeneratesRandomName_KeepingOnlyExtension()
    {
        var (service, _) = Build();

        var url = await service.SaveAsync(Content(), "anh cua toi.png", "image/png");

        url.Should().NotContain("anh cua toi");
        url.Should().EndWith(".png");
    }

    [Test]
    public async Task SaveAsync_TwoUploadsSameName_GetDifferentUrls()
    {
        var (service, _) = Build();

        var first = await service.SaveAsync(Content(), "anh.png", "image/png");
        var second = await service.SaveAsync(Content(), "anh.png", "image/png");

        first.Should().NotBe(second);
    }

    /// <summary>Kho lưu trữ từ chối thì phải nổ — nuốt lỗi sẽ lưu vào DB một URL không tồn tại.</summary>
    [Test]
    public async Task SaveAsync_ProviderRejects_Throws()
    {
        var (service, _) = Build(HttpStatusCode.Forbidden);

        Func<Task> act = () => service.SaveAsync(Content(), "anh.png", "image/png");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task SaveAsync_TrailingSlashInUrl_DoesNotProduceDoubleSlash()
    {
        var (service, _) = Build(settings: Settings with { Url = "https://proj.supabase.co/" });

        var url = await service.SaveAsync(Content(), "anh.png", "image/png");

        url.Should().NotContain("//storage");
    }

    [TestCase("", "application/octet-stream")]
    [TestCase("image/png", "image/png")]
    public async Task SaveAsync_SetsContentType(string given, string expected)
    {
        var (service, handler) = Build();

        await service.SaveAsync(Content(), "anh.png", given);

        handler.LastRequest!.Content!.Headers.ContentType!.MediaType.Should().Be(expected);
    }

    /// <summary>Chưa cấu hình thì DI không chọn service này — khẳng định điều kiện đó đúng.</summary>
    [Test]
    public void IsConfigured_RequiresBothUrlAndServiceKey()
    {
        new SupabaseStorageSettings { Url = "https://x.supabase.co" }.IsConfigured.Should().BeFalse();
        new SupabaseStorageSettings { ServiceKey = "k" }.IsConfigured.Should().BeFalse();
        new SupabaseStorageSettings { Url = "https://x.supabase.co", ServiceKey = "k" }
            .IsConfigured.Should().BeTrue();
    }
}
