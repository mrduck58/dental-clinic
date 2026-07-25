using DentalClinic.API.Application.UseCases.Medicines;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Medicines;

[TestFixture]
public class DeleteMedicineHandlerTests
{
    private IMedicineRepository _repo = null!;
    private IActivityLogService _activityLog = null!;
    private ICurrentUserService _currentUser = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IMedicineRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
    }

    /// <summary>
    /// Xóa thuốc tồn tại phải gọi DeleteAsync 1 lần với đúng entity.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingMedicine_CallsDeleteAsyncOnce()
    {
        var medicine = Medicine.Create("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh");
        _repo.GetByIdAsync(medicine.Id, Arg.Any<CancellationToken>()).Returns(medicine);
        var handler = new DeleteMedicineHandler(_repo, _activityLog, _currentUser);

        await handler.HandleAsync(medicine.Id);

        await _repo.Received(1).DeleteAsync(medicine, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Xóa thuốc không tồn tại phải ném NotFoundException, không gọi DeleteAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Medicine?)null);
        var handler = new DeleteMedicineHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Medicine>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Xóa thuốc không tồn tại không được ghi nhật ký hoạt động xóa.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_DoesNotLogActivity()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Medicine?)null);
        var handler = new DeleteMedicineHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
        await _activityLog.DidNotReceive().LogAsync(
            userId: Arg.Any<Guid?>(), userName: Arg.Any<string>(), userRole: Arg.Any<string>(),
            action: Arg.Any<string>(), module: Arg.Any<string>(), description: Arg.Any<string>(),
            status: Arg.Any<string>(), ipAddress: Arg.Any<string?>(), targetId: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Xóa thuốc tồn tại phải ghi nhật ký hoạt động đúng hành động (Delete), module (Medicine)
    /// và mô tả có chứa tên thuốc vừa xóa.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingMedicine_LogsDeleteActivityWithMedicineName()
    {
        var medicine = Medicine.Create("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh");
        _repo.GetByIdAsync(medicine.Id, Arg.Any<CancellationToken>()).Returns(medicine);
        var handler = new DeleteMedicineHandler(_repo, _activityLog, _currentUser);

        await handler.HandleAsync(medicine.Id);

        await _activityLog.Received(1).LogAsync(
            userId: Arg.Any<Guid?>(),
            userName: Arg.Any<string>(),
            userRole: Arg.Any<string>(),
            action: ActivityAction.Delete,
            module: ActivityModule.Medicine,
            description: Arg.Is<string>(d => d.Contains("Amoxicillin")),
            status: ActivityStatus.Success,
            ipAddress: Arg.Any<string?>(),
            targetId: medicine.Id.ToString(),
            ct: Arg.Any<CancellationToken>());
    }
}
