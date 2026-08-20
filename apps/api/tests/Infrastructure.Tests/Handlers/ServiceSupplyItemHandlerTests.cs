using DentalClinic.API.Application.UseCases.Services;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class ServiceSupplyItemHandlerTests
{
    private AppDbContext _db = null!;
    private ServiceSupplyItemHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new ServiceSupplyItemHandler(
            new ServiceSupplyItemRepository(_db),
            new ServiceRepository(_db),
            new SupplyItemRepository(_db));
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

    private async Task<SupplyItem> SeedSupplyItemAsync(string name = "Găng tay y tế")
    {
        var item = SupplyItem.Create("VT-001", name, "Vật dụng", "Hộp", 100, 10);
        _db.SupplyItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    private async Task<Service> SeedServiceWithOptionsAsync(params string[] optionNames)
    {
        var service = Service.Create("Bọc răng sứ", 3_000_000m, 60, "Bọc răng sứ thẩm mỹ");
        foreach (var name in optionNames)
            service.AddOption(name, 3_000_000m, "Răng", 0);
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        return service;
    }

    [Test]
    public async Task GetByServiceAsync_NoItems_ReturnsEmptyList()
    {
        var service = await SeedServiceAsync();

        var result = await _handler.Handle(new GetServiceSupplyItemsQuery(service.Id), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetByServiceAsync_ReturnsItemsWithSupplyItemNameAndUnit()
    {
        var service = await SeedServiceAsync();
        var item = await SeedSupplyItemAsync();
        _db.ServiceSupplyItems.Add(ServiceSupplyItem.Create(service.Id, item.Id, 2));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetServiceSupplyItemsQuery(service.Id), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].SupplyItemName.Should().Be("Găng tay y tế");
        result[0].Unit.Should().Be("Hộp");
        result[0].DefaultQuantity.Should().Be(2);
    }

    [Test]
    public async Task ReplaceForServiceAsync_ServiceNotFound_ThrowsNotFoundException()
    {
        var item = await SeedSupplyItemAsync();
        var request = new List<ServiceSupplyItemStepRequest> { new(item.Id, 1) };

        Func<Task> act = () => _handler.Handle(new ReplaceServiceSupplyItemsCommand(Guid.NewGuid(), request), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ReplaceForServiceAsync_SupplyItemNotFound_ThrowsNotFoundException()
    {
        var service = await SeedServiceAsync();
        var request = new List<ServiceSupplyItemStepRequest> { new(Guid.NewGuid(), 1) };

        Func<Task> act = () => _handler.Handle(new ReplaceServiceSupplyItemsCommand(service.Id, request), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ReplaceForServiceAsync_DuplicateSupplyItem_ThrowsValidationException()
    {
        var service = await SeedServiceAsync();
        var item = await SeedSupplyItemAsync();
        var request = new List<ServiceSupplyItemStepRequest> { new(item.Id, 1), new(item.Id, 2) };

        Func<Task> act = () => _handler.Handle(new ReplaceServiceSupplyItemsCommand(service.Id, request), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ReplaceForServiceAsync_NonPositiveQuantity_ThrowsValidationException()
    {
        var service = await SeedServiceAsync();
        var item = await SeedSupplyItemAsync();
        var request = new List<ServiceSupplyItemStepRequest> { new(item.Id, 0) };

        Func<Task> act = () => _handler.Handle(new ReplaceServiceSupplyItemsCommand(service.Id, request), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ReplaceForServiceAsync_ValidRequest_ReplacesOldItemsWithNewOnes()
    {
        var service = await SeedServiceAsync();
        var oldItem = await SeedSupplyItemAsync("Vật tư cũ");
        var newItem = await SeedSupplyItemAsync("Vật tư mới");
        _db.ServiceSupplyItems.Add(ServiceSupplyItem.Create(service.Id, oldItem.Id, 1));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(
            new ReplaceServiceSupplyItemsCommand(service.Id, [new(newItem.Id, 3)]), CancellationToken.None);

        result.Should().ContainSingle(i => i.SupplyItemName == "Vật tư mới" && i.DefaultQuantity == 3);
        (await _db.ServiceSupplyItems.CountAsync(s => s.ServiceId == service.Id)).Should().Be(1);
    }

    /// <summary>Cùng một vật tư được khai 2 lần với 2 option KHÁC NHAU (vd: sứ Titan x1, sứ Zirconia x1)
    /// phải được chấp nhận — chỉ trùng lặp thật (cùng vật tư + cùng option) mới bị từ chối.</summary>
    [Test]
    public async Task ReplaceForServiceAsync_SameSupplyItemDifferentOptions_IsAllowed()
    {
        var service = await SeedServiceWithOptionsAsync("Titan", "Zirconia");
        var item = await SeedSupplyItemAsync("Răng sứ");
        var request = new List<ServiceSupplyItemStepRequest> { new(item.Id, 1, "Titan"), new(item.Id, 1, "Zirconia") };

        var result = await _handler.Handle(new ReplaceServiceSupplyItemsCommand(service.Id, request), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(i => i.ServiceOptionName == "Titan");
        result.Should().Contain(i => i.ServiceOptionName == "Zirconia");
    }

    [Test]
    public async Task ReplaceForServiceAsync_SameSupplyItemSameOption_ThrowsValidationException()
    {
        var service = await SeedServiceWithOptionsAsync("Titan");
        var item = await SeedSupplyItemAsync();
        var request = new List<ServiceSupplyItemStepRequest> { new(item.Id, 1, "Titan"), new(item.Id, 2, "Titan") };

        Func<Task> act = () => _handler.Handle(new ReplaceServiceSupplyItemsCommand(service.Id, request), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Option chỉ định không tồn tại trong danh sách option hiện tại của dịch vụ phải bị từ chối
    /// (tránh khai định mức cho một option đã bị xóa/đổi tên khi sửa dịch vụ).</summary>
    [Test]
    public async Task ReplaceForServiceAsync_OptionNameNotInServiceOptions_ThrowsValidationException()
    {
        var service = await SeedServiceWithOptionsAsync("Titan");
        var item = await SeedSupplyItemAsync();
        var request = new List<ServiceSupplyItemStepRequest> { new(item.Id, 1, "Không tồn tại") };

        Func<Task> act = () => _handler.Handle(new ReplaceServiceSupplyItemsCommand(service.Id, request), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Định mức HIỆU LỰC cho một option cụ thể = dòng dùng chung + dòng khai riêng cho đúng option đó,
    /// KHÔNG gồm dòng khai riêng cho option khác.</summary>
    [Test]
    public async Task GetEffectiveByServiceAsync_ReturnsGeneralRowsPlusMatchingOptionRows_ExcludesOtherOptions()
    {
        var service = await SeedServiceWithOptionsAsync("Titan", "Zirconia");
        var gloves = await SeedSupplyItemAsync("Găng tay");
        var titanCrown = await SeedSupplyItemAsync("Sứ Titan");
        var zirconiaCrown = await SeedSupplyItemAsync("Sứ Zirconia");
        await _handler.Handle(new ReplaceServiceSupplyItemsCommand(service.Id,
        [
            new(gloves.Id, 1),
            new(titanCrown.Id, 1, "Titan"),
            new(zirconiaCrown.Id, 1, "Zirconia"),
        ]), CancellationToken.None);

        var result = await _handler.Handle(new GetEffectiveServiceSupplyItemsQuery(service.Id, "Titan"), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(i => i.SupplyItemName == "Găng tay");
        result.Should().Contain(i => i.SupplyItemName == "Sứ Titan");
        result.Should().NotContain(i => i.SupplyItemName == "Sứ Zirconia");
    }

    /// <summary>Không chọn option nào (dịch vụ dùng giá gốc) chỉ trả về các dòng dùng chung.</summary>
    [Test]
    public async Task GetEffectiveByServiceAsync_NoOptionSelected_ReturnsOnlyGeneralRows()
    {
        var service = await SeedServiceWithOptionsAsync("Titan");
        var gloves = await SeedSupplyItemAsync("Găng tay");
        var titanCrown = await SeedSupplyItemAsync("Sứ Titan");
        await _handler.Handle(new ReplaceServiceSupplyItemsCommand(service.Id,
        [
            new(gloves.Id, 1),
            new(titanCrown.Id, 1, "Titan"),
        ]), CancellationToken.None);

        var result = await _handler.Handle(new GetEffectiveServiceSupplyItemsQuery(service.Id, null), CancellationToken.None);

        result.Should().ContainSingle(i => i.SupplyItemName == "Găng tay");
    }
}
