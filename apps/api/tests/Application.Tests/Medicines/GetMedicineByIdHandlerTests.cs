using DentalClinic.API.Application.UseCases.Medicines;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Medicines;

[TestFixture]
public class GetMedicineByIdHandlerTests
{
    private IMedicineRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IMedicineRepository>();

    /// <summary>
    /// Lấy thuốc theo ID hợp lệ phải trả về DTO với đầy đủ thông tin.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingMedicine_ReturnsDto()
    {
        var medicine = Medicine.Create("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh");
        _repo.GetByIdAsync(medicine.Id, Arg.Any<CancellationToken>()).Returns(medicine);
        var handler = new GetMedicineByIdHandler(_repo);

        var result = await handler.HandleAsync(medicine.Id);

        result.Id.Should().Be(medicine.Id);
        result.Name.Should().Be("Amoxicillin");
    }

    /// <summary>
    /// ID không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Medicine?)null);
        var handler = new GetMedicineByIdHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
