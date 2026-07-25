using DentalClinic.API.Application.UseCases.Services;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class TreatmentProcedureHandlerTests
{
    private AppDbContext _db = null!;
    private TreatmentProcedureHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new TreatmentProcedureHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<Service> SeedServiceAsync()
    {
        var service = Service.Create("Trồng răng Implant", 15_000_000m, 90, "Cấy ghép implant");
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        return service;
    }

    /// <summary>Dịch vụ chưa có quy trình nào phải trả về danh sách rỗng.</summary>
    [Test]
    public async Task GetByServiceAsync_NoProcedures_ReturnsEmptyList()
    {
        var service = await SeedServiceAsync();

        var result = await _handler.GetByServiceAsync(service.Id);

        result.Should().BeEmpty();
    }

    /// <summary>Danh sách quy trình phải được trả về theo đúng thứ tự số bước (StepNumber).</summary>
    [Test]
    public async Task GetByServiceAsync_ReturnsStepsOrderedByStepNumber()
    {
        var service = await SeedServiceAsync();
        _db.TreatmentProcedures.Add(TreatmentProcedure.Create(service.Id, 2, "Đặt trụ Implant"));
        _db.TreatmentProcedures.Add(TreatmentProcedure.Create(service.Id, 1, "Khám và chụp X-quang"));
        await _db.SaveChangesAsync();

        var result = await _handler.GetByServiceAsync(service.Id);

        result.Should().HaveCount(2);
        result[0].StepNumber.Should().Be(1);
        result[1].StepNumber.Should().Be(2);
    }

    /// <summary>Dịch vụ không tồn tại (ID ngẫu nhiên) phải trả về danh sách rỗng, không ném lỗi
    /// (khác với ReplaceForServiceAsync — GetByServiceAsync không kiểm tra sự tồn tại của dịch vụ).</summary>
    [Test]
    public async Task GetByServiceAsync_ServiceDoesNotExist_ReturnsEmptyListWithoutThrowing()
    {
        var result = await _handler.GetByServiceAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    /// <summary>Chỉ trả về các bước điều trị của đúng dịch vụ được yêu cầu, không lẫn dịch vụ khác.</summary>
    [Test]
    public async Task GetByServiceAsync_OnlyReturnsProceduresForRequestedService()
    {
        var service = await SeedServiceAsync();
        var otherService = Service.Create("Tẩy trắng", 800_000m, 60, "Làm sáng răng");
        _db.Services.Add(otherService);
        _db.TreatmentProcedures.Add(TreatmentProcedure.Create(service.Id, 1, "Bước của dịch vụ chính"));
        _db.TreatmentProcedures.Add(TreatmentProcedure.Create(otherService.Id, 1, "Bước của dịch vụ khác"));
        await _db.SaveChangesAsync();

        var result = await _handler.GetByServiceAsync(service.Id);

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Bước của dịch vụ chính");
    }

    /// <summary>Thay quy trình cho dịch vụ không tồn tại phải báo lỗi NotFoundException.</summary>
    [Test]
    public async Task ReplaceForServiceAsync_ServiceNotFound_ThrowsNotFoundException()
    {
        var steps = new List<ProcedureStepRequest> { new(1, "Bước 1") };

        Func<Task> act = () => _handler.ReplaceForServiceAsync(Guid.NewGuid(), steps);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Tên bước điều trị để trống phải bị từ chối.</summary>
    [Test]
    public async Task ReplaceForServiceAsync_StepWithEmptyName_ThrowsValidationException()
    {
        var service = await SeedServiceAsync();
        var steps = new List<ProcedureStepRequest> { new(1, "  ") };

        Func<Task> act = () => _handler.ReplaceForServiceAsync(service.Id, steps);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Hai bước trùng số thứ tự phải bị từ chối.</summary>
    [Test]
    public async Task ReplaceForServiceAsync_DuplicateStepNumbers_ThrowsValidationException()
    {
        var service = await SeedServiceAsync();
        var steps = new List<ProcedureStepRequest> { new(1, "Bước A"), new(1, "Bước B") };

        Func<Task> act = () => _handler.ReplaceForServiceAsync(service.Id, steps);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Thay quy trình hợp lệ phải xóa toàn bộ bước cũ và lưu đúng các bước mới.</summary>
    [Test]
    public async Task ReplaceForServiceAsync_ValidRequest_ReplacesOldStepsWithNewOnes()
    {
        var service = await SeedServiceAsync();
        _db.TreatmentProcedures.Add(TreatmentProcedure.Create(service.Id, 1, "Bước Cũ"));
        await _db.SaveChangesAsync();

        var newSteps = new List<ProcedureStepRequest> { new(1, "Bước Mới 1"), new(2, "Bước Mới 2") };
        var result = await _handler.ReplaceForServiceAsync(service.Id, newSteps);

        result.Should().HaveCount(2);
        result.Should().NotContain(p => p.Name == "Bước Cũ");
        (await _db.TreatmentProcedures.CountAsync(p => p.ServiceId == service.Id)).Should().Be(2);
    }

    /// <summary>Danh sách bước rỗng phải xóa toàn bộ quy trình cũ và trả về danh sách rỗng.</summary>
    [Test]
    public async Task ReplaceForServiceAsync_EmptyStepsList_RemovesAllExistingProcedures()
    {
        var service = await SeedServiceAsync();
        _db.TreatmentProcedures.Add(TreatmentProcedure.Create(service.Id, 1, "Bước Cũ"));
        await _db.SaveChangesAsync();

        var result = await _handler.ReplaceForServiceAsync(service.Id, []);

        result.Should().BeEmpty();
        (await _db.TreatmentProcedures.CountAsync(p => p.ServiceId == service.Id)).Should().Be(0);
    }

    /// <summary>Tên bước có khoảng trắng đầu/cuối phải được cắt gọn (Trim) trước khi lưu.</summary>
    [Test]
    public async Task ReplaceForServiceAsync_StepNameWithSurroundingWhitespace_IsTrimmedBeforeSaving()
    {
        var service = await SeedServiceAsync();
        var steps = new List<ProcedureStepRequest> { new(1, "  Khám tổng quát  ") };

        var result = await _handler.ReplaceForServiceAsync(service.Id, steps);

        result.Should().ContainSingle(p => p.Name == "Khám tổng quát");
    }

    /// <summary>Thay quy trình của một dịch vụ không được ảnh hưởng đến quy trình của dịch vụ khác.</summary>
    [Test]
    public async Task ReplaceForServiceAsync_DoesNotAffectOtherServicesProcedures()
    {
        var service = await SeedServiceAsync();
        var otherService = Service.Create("Tẩy trắng", 800_000m, 60, "Làm sáng răng");
        _db.Services.Add(otherService);
        _db.TreatmentProcedures.Add(TreatmentProcedure.Create(otherService.Id, 1, "Bước dịch vụ khác"));
        await _db.SaveChangesAsync();

        await _handler.ReplaceForServiceAsync(service.Id, [new(1, "Bước dịch vụ chính")]);

        var otherProcedures = await _handler.GetByServiceAsync(otherService.Id);
        otherProcedures.Should().ContainSingle(p => p.Name == "Bước dịch vụ khác");
    }
}
