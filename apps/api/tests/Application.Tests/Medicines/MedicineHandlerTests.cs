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
public class MedicineHandlerTests
{
    private IMedicineRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IMedicineRepository>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateMedicineHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tạo thuốc với đầy đủ thông tin phải gọi AddAsync 1 lần và trả về MedicineDto.
    /// </summary>
    [Test]
    public async Task Create_ValidRequest_CallsAddAsyncAndReturnsDto()
    {
        var handler = new CreateMedicineHandler(_repo);

        var result = await handler.HandleAsync(BuildCreateRequest("Amoxicillin", "Amoxicillin", "GSK"));

        await _repo.Received(1).AddAsync(Arg.Any<Medicine>(), Arg.Any<CancellationToken>());
        result.Name.Should().Be("Amoxicillin");
        result.GenericName.Should().Be("Amoxicillin");
        result.Manufacturer.Should().Be("GSK");
    }

    /// <summary>
    /// Thuốc mới tạo phải có Id khác rỗng và CreatedAt được thiết lập.
    /// </summary>
    [Test]
    public async Task Create_NewMedicine_HasValidIdAndCreatedAt()
    {
        var handler = new CreateMedicineHandler(_repo);

        var result = await handler.HandleAsync(BuildCreateRequest("Paracetamol", "Paracetamol", "Pharma"));

        result.Id.Should().NotBe(Guid.Empty);
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tất cả các trường trong request phải được ánh xạ đúng vào DTO trả về.
    /// </summary>
    [Test]
    public async Task Create_MapsAllFieldsCorrectly()
    {
        var handler = new CreateMedicineHandler(_repo);
        var req = new CreateMedicineRequest("Ibuprofen", "Ibuprofen BP", "Pfizer", "Viên", "Giảm đau hạ sốt");

        var result = await handler.HandleAsync(req);

        result.Name.Should().Be("Ibuprofen");
        result.GenericName.Should().Be("Ibuprofen BP");
        result.Manufacturer.Should().Be("Pfizer");
        result.Unit.Should().Be("Viên");
        result.Description.Should().Be("Giảm đau hạ sốt");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdateMedicineHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cập nhật thuốc tồn tại phải gọi UpdateAsync 1 lần và trả về DTO với thông tin mới.
    /// </summary>
    [Test]
    public async Task Update_ExistingMedicine_CallsUpdateAsyncAndReturnsUpdatedDto()
    {
        var medicine = MakeMedicine("Amoxicillin");
        _repo.GetByIdAsync(medicine.Id, Arg.Any<CancellationToken>()).Returns(medicine);
        var handler = new UpdateMedicineHandler(_repo);

        var result = await handler.HandleAsync(medicine.Id, BuildUpdateRequest("Amoxicillin 500mg", "Amox", "Novartis"));

        await _repo.Received(1).UpdateAsync(medicine, Arg.Any<CancellationToken>());
        result.Name.Should().Be("Amoxicillin 500mg");
        result.Manufacturer.Should().Be("Novartis");
    }

    /// <summary>
    /// Cập nhật thuốc không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task Update_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Medicine?)null);
        var handler = new UpdateMedicineHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), BuildUpdateRequest("X", "X", "X"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Sau khi cập nhật, UpdatedAt phải được thiết lập.
    /// </summary>
    [Test]
    public async Task Update_ExistingMedicine_SetsUpdatedAt()
    {
        var medicine = MakeMedicine();
        _repo.GetByIdAsync(medicine.Id, Arg.Any<CancellationToken>()).Returns(medicine);
        var handler = new UpdateMedicineHandler(_repo);

        var result = await handler.HandleAsync(medicine.Id, BuildUpdateRequest("Mới", "Mới", "Mới"));

        result.UpdatedAt.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DeleteMedicineHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Xóa thuốc tồn tại phải gọi DeleteAsync 1 lần với đúng entity.
    /// </summary>
    [Test]
    public async Task Delete_ExistingMedicine_CallsDeleteAsyncOnce()
    {
        var medicine = MakeMedicine();
        _repo.GetByIdAsync(medicine.Id, Arg.Any<CancellationToken>()).Returns(medicine);
        var handler = new DeleteMedicineHandler(_repo);

        await handler.HandleAsync(medicine.Id);

        await _repo.Received(1).DeleteAsync(medicine, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Xóa thuốc không tồn tại phải ném NotFoundException, không gọi DeleteAsync.
    /// </summary>
    [Test]
    public async Task Delete_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Medicine?)null);
        var handler = new DeleteMedicineHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Medicine>(), Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMedicinesHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Không có filter trả về toàn bộ danh sách thuốc.
    /// </summary>
    [Test]
    public async Task GetMedicines_NoFilter_ReturnsAll()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Medicine>
        {
            MakeMedicine("Amoxicillin"), MakeMedicine("Paracetamol"), MakeMedicine("Ibuprofen"),
        });
        var handler = new GetMedicinesHandler(_repo);

        var result = await handler.HandleAsync(null);

        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Tìm kiếm theo tên thuốc không phân biệt hoa thường.
    /// </summary>
    [Test]
    public async Task GetMedicines_SearchByName_ReturnsMatchingMedicines()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Medicine>
        {
            MakeMedicine("Amoxicillin"), MakeMedicine("Paracetamol"), MakeMedicine("Amoxiclav"),
        });
        var handler = new GetMedicinesHandler(_repo);

        var result = await handler.HandleAsync(search: "amox");

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Tìm kiếm theo tên gốc (GenericName) cũng phải hoạt động.
    /// </summary>
    [Test]
    public async Task GetMedicines_SearchByGenericName_ReturnsMatchingMedicines()
    {
        var m1 = Medicine.Create("Doliprane", "Paracetamol", "Sanofi", "Viên", "Hạ sốt");
        var m2 = Medicine.Create("Hapacol", "Paracetamol 500mg", "DHG", "Gói", "Giảm đau");
        var m3 = Medicine.Create("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh");
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Medicine> { m1, m2, m3 });
        var handler = new GetMedicinesHandler(_repo);

        var result = await handler.HandleAsync(search: "paracetamol");

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Tìm kiếm theo nhà sản xuất cũng phải hoạt động.
    /// </summary>
    [Test]
    public async Task GetMedicines_SearchByManufacturer_ReturnsMatchingMedicines()
    {
        var m1 = Medicine.Create("Thuốc A", "Generic A", "Sanofi", "Viên", "Mô tả");
        var m2 = Medicine.Create("Thuốc B", "Generic B", "GSK", "Gói", "Mô tả");
        var m3 = Medicine.Create("Thuốc C", "Generic C", "Sanofi", "Lọ", "Mô tả");
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Medicine> { m1, m2, m3 });
        var handler = new GetMedicinesHandler(_repo);

        var result = await handler.HandleAsync(search: "sanofi");

        result.Should().HaveCount(2);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMedicineByIdHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy thuốc theo ID hợp lệ phải trả về DTO với đầy đủ thông tin.
    /// </summary>
    [Test]
    public async Task GetById_ExistingMedicine_ReturnsDto()
    {
        var medicine = MakeMedicine("Amoxicillin");
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
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Medicine?)null);
        var handler = new GetMedicineByIdHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static Medicine MakeMedicine(string name = "Thuốc Test")
        => Medicine.Create(name, name, "Nhà Sản Xuất", "Viên", "Mô tả thuốc");

    private static CreateMedicineRequest BuildCreateRequest(string name, string generic, string mfr)
        => new(name, generic, mfr, "Viên", "Mô tả");

    private static UpdateMedicineRequest BuildUpdateRequest(string name, string generic, string mfr)
        => new(name, generic, mfr, "Viên", "Mô tả cập nhật");
}
