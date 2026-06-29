using DentalClinic.API.Application.UseCases.Medicines;
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
}
