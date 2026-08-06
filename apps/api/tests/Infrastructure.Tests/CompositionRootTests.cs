using DentalClinic.API.Application.DependencyInjection;
using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Application.UseCases.Invoices;
using DentalClinic.API.Application.UseCases.Queue;
using DentalClinic.API.Infrastructure.Extensions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ValidationException = DentalClinic.API.Domain.Exceptions.ValidationException;

namespace DentalClinic.API.Infrastructure.Tests;

/// <summary>
/// Dựng đúng DI container thật (AddApplication + AddInfrastructure) rồi thử resolve ISender và gửi
/// 1 request thật — bài test unit/integration thường KHÔNG đi qua container này (new Handler(...) trực
/// tiếp) nên không tự phát hiện được lỗi "MediatR quét sai assembly" (ví dụ quét Infrastructure trong
/// khi toàn bộ handler nằm ở Application, sau khi tách 4 project) — lỗi loại này chỉ lộ ra ở runtime
/// khi Controller gọi ISender.Send thật. Test này khóa lại đúng hành vi composition root.
/// </summary>
[TestFixture]
public class CompositionRootTests
{
    [Test]
    public void AddApplication_And_AddInfrastructure_RegisterMediatRHandlersFromApplicationAssembly()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["JwtSettings:Secret"] = "unit-test-secret-key-not-used-for-signing-anything-real",
            })
            .Build();

        services.AddApplication();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull("AddApplication() phải đăng ký được MediatR ISender.");

        // Resolve trực tiếp 1 handler đại diện (KHÔNG gọi Send/Handle — tránh cần kết nối DB thật) để
        // xác nhận MediatR đã quét đúng assembly Application và tìm thấy handler thật, không phải chỉ
        // ISender tồn tại suông. Đây chính là điểm trước đây quét sai assembly Infrastructure sau khi
        // tách project mà không lộ ra qua bất kỳ unit test dựng handler trực tiếp nào.
        var handler = provider.GetService<IRequestHandler<GetOutstandingInvoicesQuery, List<InvoiceDto>>>();
        handler.Should().NotBeNull(
            "MediatR phải quét được GetOutstandingInvoicesHandler từ assembly Application.");
    }

    /// <summary>Xác nhận ValidationBehavior + validator (Giai đoạn 4) thực sự chặn request KHÔNG hợp lệ
    /// ngay tại pipeline, trước khi vào handler — RoomName rỗng không cần chạm DB để phát hiện.</summary>
    [Test]
    public async Task Pipeline_InvalidCommand_ThrowsValidationExceptionBeforeReachingHandler()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["JwtSettings:Secret"] = "unit-test-secret-key-not-used-for-signing-anything-real",
            })
            .Build();

        services.AddApplication();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        Func<Task> act = () => sender.Send(
            new TransferQueuePatientCommand(Guid.NewGuid(), "  "), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
