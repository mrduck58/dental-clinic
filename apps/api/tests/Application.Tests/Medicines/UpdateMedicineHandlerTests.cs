using DentalClinic.API.Application.DTOs.Medicines;
using DentalClinic.API.Application.UseCases.Medicines;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Medicines;

[TestFixture]
public class UpdateMedicineHandlerTests
{
    private IMedicineRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IMedicineRepository>();

    /// <summary>
    /// Cập nhật thuốc tồn tại phải gọi UpdateAsync và trả về DTO với thông tin mới.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingMedicine_CallsUpdateAsyncAndReturnsUpdatedDto()
    {
        var medicine = Medicine.Create("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh");
        _repo.GetByIdAsync(medicine.Id, Arg.Any<CancellationToken>()).Returns(medicine);
        var handler = new UpdateMedicineHandler(_repo);

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
        var handler = new UpdateMedicineHandler(_repo);

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
        var handler = new UpdateMedicineHandler(_repo);

        var result = await handler.HandleAsync(medicine.Id, new UpdateMedicineRequest("Mới", "Mới", "Mới", "Viên", "Mới"));

        result.UpdatedAt.Should().NotBeNull();
    }
}
