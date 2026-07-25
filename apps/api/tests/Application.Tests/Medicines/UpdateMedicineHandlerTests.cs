using DentalClinic.API.Application.DTOs.Medicines;
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
public class UpdateMedicineHandlerTests
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
    /// Cập nhật thuốc tồn tại phải gọi UpdateAsync và trả về DTO với thông tin mới.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingMedicine_CallsUpdateAsyncAndReturnsUpdatedDto()
    {
        var medicine = Medicine.Create("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh");
        _repo.GetByIdAsync(medicine.Id, Arg.Any<CancellationToken>()).Returns(medicine);
        var handler = new UpdateMedicineHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(medicine.Id, new UpdateMedicineRequest("Amoxicillin 500mg", "Amox", "Novartis", "Viên", "Kháng sinh"));

        await _repo.Received(1).UpdateAsync(medicine, Arg.Any<CancellationToken>());
        result.Name.Should().Be("Amoxicillin 500mg");
        result.Manufacturer.Should().Be("Novartis");
    }

    /// <summary>
    /// Thuốc không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Medicine?)null);
        var handler = new UpdateMedicineHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), new UpdateMedicineRequest("X", "X", "X", "Viên", "X"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Sau khi cập nhật, UpdatedAt phải được thiết lập.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingMedicine_SetsUpdatedAt()
    {
        var medicine = Medicine.Create("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh");
        _repo.GetByIdAsync(medicine.Id, Arg.Any<CancellationToken>()).Returns(medicine);
        var handler = new UpdateMedicineHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(medicine.Id, new UpdateMedicineRequest("Mới", "Mới", "Mới", "Viên", "Mới"));

        result.UpdatedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Cập nhật thuốc không tồn tại không được gọi UpdateAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_DoesNotCallUpdateAsync()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Medicine?)null);
        var handler = new UpdateMedicineHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), new UpdateMedicineRequest("X", "X", "X", "Viên", "X"));

        await act.Should().ThrowAsync<NotFoundException>();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Medicine>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Cập nhật thuốc tồn tại phải ghi nhật ký hoạt động đúng hành động (Edit), module (Medicine)
    /// và mô tả có chứa tên thuốc sau khi cập nhật.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingMedicine_LogsEditActivityWithUpdatedName()
    {
        var medicine = Medicine.Create("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh");
        _repo.GetByIdAsync(medicine.Id, Arg.Any<CancellationToken>()).Returns(medicine);
        var handler = new UpdateMedicineHandler(_repo, _activityLog, _currentUser);

        await handler.HandleAsync(medicine.Id, new UpdateMedicineRequest("Amoxicillin 500mg", "Amox", "Novartis", "Viên", "Kháng sinh"));

        await _activityLog.Received(1).LogAsync(
            userId: Arg.Any<Guid?>(),
            userName: Arg.Any<string>(),
            userRole: Arg.Any<string>(),
            action: ActivityAction.Edit,
            module: ActivityModule.Medicine,
            description: Arg.Is<string>(d => d.Contains("Amoxicillin 500mg")),
            status: ActivityStatus.Success,
            ipAddress: Arg.Any<string?>(),
            targetId: medicine.Id.ToString(),
            ct: Arg.Any<CancellationToken>());
    }
}
