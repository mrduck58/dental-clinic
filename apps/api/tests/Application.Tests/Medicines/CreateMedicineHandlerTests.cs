using DentalClinic.API.Application.DTOs.Medicines;
using DentalClinic.API.Application.UseCases.Medicines;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Medicines;

[TestFixture]
public class CreateMedicineHandlerTests
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
    /// Tạo thuốc với đầy đủ thông tin phải gọi AddAsync 1 lần và trả về MedicineDto.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_CallsAddAsyncAndReturnsDto()
    {
        var handler = new CreateMedicineHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(new CreateMedicineRequest("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh"));

        await _repo.Received(1).AddAsync(Arg.Any<Medicine>(), Arg.Any<CancellationToken>());
        result.Name.Should().Be("Amoxicillin");
        result.Manufacturer.Should().Be("GSK");
    }

    /// <summary>
    /// Thuốc mới tạo phải có Id khác rỗng và CreatedAt được thiết lập.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewMedicine_HasValidIdAndCreatedAt()
    {
        var handler = new CreateMedicineHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(new CreateMedicineRequest("Paracetamol", "Paracetamol", "Pharma", "Viên", "Hạ sốt"));

        result.Id.Should().NotBe(Guid.Empty);
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tất cả các trường trong request phải được ánh xạ đúng vào DTO trả về.
    /// </summary>
    [Test]
    public async Task HandleAsync_MapsAllFieldsCorrectly()
    {
        var handler = new CreateMedicineHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(new CreateMedicineRequest("Ibuprofen", "Ibuprofen BP", "Pfizer", "Viên", "Giảm đau"));

        result.GenericName.Should().Be("Ibuprofen BP");
        result.Unit.Should().Be("Viên");
        result.Description.Should().Be("Giảm đau");
    }
}
