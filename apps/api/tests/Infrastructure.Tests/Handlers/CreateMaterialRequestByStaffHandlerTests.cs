using DentalClinic.API.Application.UseCases.Inventory;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class CreateMaterialRequestByStaffHandlerTests
{
    private AppDbContext _db = null!;
    private CreateMaterialRequestByStaffHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new CreateMaterialRequestByStaffHandler(new MaterialRequestRepository(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task HandleAsync_EmptyItems_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.Handle(
            new CreateMaterialRequestByStaffRequest(Guid.NewGuid(), "Nguyễn Văn A", "Đặt răng sứ", []),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task HandleAsync_NoPatientSelected_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.Handle(
            new CreateMaterialRequestByStaffRequest(Guid.Empty, "", "Đặt răng sứ", [new MaterialRequestItemInput("Răng sứ", 1, "Cái")]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task HandleAsync_BlankDescription_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.Handle(
            new CreateMaterialRequestByStaffRequest(Guid.NewGuid(), "Nguyễn Văn A", "  ", [new MaterialRequestItemInput("Răng sứ", 1, "Cái")]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Yêu cầu hợp lệ do staff tự tạo phải lưu đúng tên bệnh nhân (do FE gửi kèm) làm mô tả/CourseName,
    /// DentistName để trống (không gắn với buổi khám hay bác sĩ nào), trạng thái Pending.</summary>
    [Test]
    public async Task HandleAsync_ValidRequest_CreatesRequestWithoutDentist()
    {
        var patientId = Guid.NewGuid();

        var result = await _handler.Handle(
            new CreateMaterialRequestByStaffRequest(patientId, "Nguyễn Văn A", "Đặt răng sứ Zirconia", [
                new MaterialRequestItemInput("Răng sứ Zirconia", 2, "Cái"),
            ]),
            CancellationToken.None);

        result.PatientName.Should().Be("Nguyễn Văn A");
        result.CourseName.Should().Be("Đặt răng sứ Zirconia");
        result.DentistName.Should().BeEmpty();
        result.Status.Should().Be("Pending");
        result.Items.Should().ContainSingle(i => i.ItemName == "Răng sứ Zirconia" && i.Quantity == 2);

        var persisted = await _db.MaterialRequests.SingleAsync(m => m.Id == result.Id);
        persisted.PatientId.Should().Be(patientId);
    }
}
