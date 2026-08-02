using DentalClinic.API.Application.UseCases.Medicines;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Medicines;

[TestFixture]
public class GetMedicinesHandlerTests
{
    private IMedicineRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IMedicineRepository>();

    /// <summary>
    /// Không có filter trả về toàn bộ danh sách thuốc.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoFilter_ReturnsAll()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Medicine>
        {
            Medicine.Create("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh"),
            Medicine.Create("Paracetamol", "Paracetamol", "Pharma", "Viên", "Hạ sốt"),
            Medicine.Create("Ibuprofen", "Ibuprofen", "Pfizer", "Viên", "Giảm đau"),
        });
        var handler = new GetMedicinesHandler(_repo);

        var result = await handler.Handle(new GetMedicinesQuery(null), CancellationToken.None);

        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Tìm kiếm theo tên thuốc không phân biệt hoa thường.
    /// </summary>
    [Test]
    public async Task HandleAsync_SearchByName_ReturnsMatchingMedicines()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Medicine>
        {
            Medicine.Create("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh"),
            Medicine.Create("Paracetamol", "Paracetamol", "Pharma", "Viên", "Hạ sốt"),
            Medicine.Create("Amoxiclav", "Amoxicillin+Clavulanate", "Novartis", "Viên", "Kháng sinh"),
        });
        var handler = new GetMedicinesHandler(_repo);

        var result = await handler.Handle(new GetMedicinesQuery(Search: "amox"), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Tìm kiếm theo tên gốc (GenericName) cũng phải hoạt động.
    /// </summary>
    [Test]
    public async Task HandleAsync_SearchByGenericName_ReturnsMatchingMedicines()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Medicine>
        {
            Medicine.Create("Doliprane", "Paracetamol", "Sanofi", "Viên", "Hạ sốt"),
            Medicine.Create("Hapacol", "Paracetamol 500mg", "DHG", "Gói", "Giảm đau"),
            Medicine.Create("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh"),
        });
        var handler = new GetMedicinesHandler(_repo);

        var result = await handler.Handle(new GetMedicinesQuery(Search: "paracetamol"), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Tìm kiếm theo nhà sản xuất cũng phải hoạt động.
    /// </summary>
    [Test]
    public async Task HandleAsync_SearchByManufacturer_ReturnsMatchingMedicines()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Medicine>
        {
            Medicine.Create("Thuốc A", "Generic A", "Sanofi", "Viên", "Mô tả"),
            Medicine.Create("Thuốc B", "Generic B", "GSK", "Gói", "Mô tả"),
            Medicine.Create("Thuốc C", "Generic C", "Sanofi", "Lọ", "Mô tả"),
        });
        var handler = new GetMedicinesHandler(_repo);

        var result = await handler.Handle(new GetMedicinesQuery(Search: "sanofi"), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Chuỗi tìm kiếm rỗng phải được xem như không có filter, trả về toàn bộ danh sách.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptySearch_ReturnsAll()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Medicine>
        {
            Medicine.Create("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh"),
            Medicine.Create("Paracetamol", "Paracetamol", "Pharma", "Viên", "Hạ sốt"),
        });
        var handler = new GetMedicinesHandler(_repo);

        var result = await handler.Handle(new GetMedicinesQuery(Search: ""), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Chuỗi tìm kiếm chỉ gồm khoảng trắng phải được xem như không có filter, trả về toàn bộ danh sách.
    /// </summary>
    [Test]
    public async Task HandleAsync_WhitespaceSearch_ReturnsAll()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Medicine>
        {
            Medicine.Create("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh"),
            Medicine.Create("Paracetamol", "Paracetamol", "Pharma", "Viên", "Hạ sốt"),
        });
        var handler = new GetMedicinesHandler(_repo);

        var result = await handler.Handle(new GetMedicinesQuery(Search: "   "), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Tìm kiếm không khớp thuốc nào phải trả về danh sách rỗng, không lỗi.
    /// </summary>
    [Test]
    public async Task HandleAsync_SearchNoMatch_ReturnsEmpty()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Medicine>
        {
            Medicine.Create("Amoxicillin", "Amoxicillin", "GSK", "Viên", "Kháng sinh"),
            Medicine.Create("Paracetamol", "Paracetamol", "Pharma", "Viên", "Hạ sốt"),
        });
        var handler = new GetMedicinesHandler(_repo);

        var result = await handler.Handle(new GetMedicinesQuery(Search: "khôngtồntại"), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
