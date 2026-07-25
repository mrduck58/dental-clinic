using DentalClinic.API.Application.DTOs.ClinicInfo;
using DentalClinic.API.Application.UseCases.ClinicInfo;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Entity = DentalClinic.API.Domain.Entities.ClinicInfo;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.ClinicInfo;

[TestFixture]
public class UpdateClinicInfoHandlerTests
{
    private IClinicInfoRepository _repo = null!;
    private UpdateClinicInfoHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IClinicInfoRepository>();
        _handler = new UpdateClinicInfoHandler(_repo);
    }

    private static UpdateClinicInfoRequest MakeRequest() => new(
        "Về chúng tôi", "Mô tả mới", 2015, "0909999999", "new@test.com", "456 Đường XYZ",
        null, "8:00 - 20:00", null, null, null, null, null);

    /// <summary>Chưa có dòng thông tin nào (chưa seed) phải tạo mới và gọi AddAsync.</summary>
    [Test]
    public async Task HandleAsync_NoExistingInfo_CreatesNewRecord()
    {
        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns((Entity?)null);

        var result = await _handler.HandleAsync(MakeRequest());

        result.AboutTitle.Should().Be("Về chúng tôi");
        await _repo.Received(1).AddAsync(Arg.Any<Entity>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Entity>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Đã có dòng thông tin thì phải cập nhật (UpdateAsync), không tạo bản ghi mới.</summary>
    [Test]
    public async Task HandleAsync_ExistingInfo_UpdatesRecord()
    {
        var existing = Entity.Create("Cũ", "Mô tả cũ", 2000, "0281111111", "old@test.com", "Địa chỉ cũ");
        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.HandleAsync(MakeRequest());

        result.AboutTitle.Should().Be("Về chúng tôi");
        result.FoundedYear.Should().Be(2015);
        await _repo.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().AddAsync(Arg.Any<Entity>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Danh sách null trong request (vd. Milestones) phải giữ nguyên giá trị cũ, không bị xóa.</summary>
    [Test]
    public async Task HandleAsync_NullCollectionsInRequest_KeepsExistingCollections()
    {
        var existing = Entity.Create("Cũ", "Mô tả cũ", 2000, "0281111111", "old@test.com", "Địa chỉ cũ");
        existing.SetCollections(
            "[{\"Year\":2000,\"Description\":\"Thành lập\"}]", null, null, null, null);
        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.HandleAsync(MakeRequest());

        result.Milestones.Should().ContainSingle(m => m.Description == "Thành lập");
    }

    /// <summary>Danh sách rỗng ([]) trong request (khác null) phải xóa hết danh sách cũ.</summary>
    [Test]
    public async Task HandleAsync_EmptyListInRequest_ClearsExistingCollection()
    {
        var existing = Entity.Create("Cũ", "Mô tả cũ", 2000, "0281111111", "old@test.com", "Địa chỉ cũ");
        existing.SetCollections(
            "[{\"Year\":2000,\"Description\":\"Thành lập\"}]", null, null, null, null);
        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns(existing);
        var request = MakeRequest() with { Milestones = [] };

        var result = await _handler.HandleAsync(request);

        result.Milestones.Should().BeEmpty();
    }

    /// <summary>AboutImageUrl = null trong request phải giữ nguyên ảnh hiện tại, không bị xóa.</summary>
    [Test]
    public async Task HandleAsync_NullImageUrl_KeepsExistingImage()
    {
        var existing = Entity.Create("Cũ", "Mô tả cũ", 2000, "0281111111", "old@test.com", "Địa chỉ cũ",
            "https://existing.jpg");
        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns(existing);
        var request = MakeRequest() with { AboutImageUrl = null };

        var result = await _handler.HandleAsync(request);

        result.AboutImageUrl.Should().Be("https://existing.jpg");
    }

    /// <summary>AboutImageUrl = "" (chuỗi rỗng) trong request phải xóa ảnh hiện tại (khác với null).</summary>
    [Test]
    public async Task HandleAsync_EmptyStringImageUrl_ClearsExistingImage()
    {
        var existing = Entity.Create("Cũ", "Mô tả cũ", 2000, "0281111111", "old@test.com", "Địa chỉ cũ",
            "https://existing.jpg");
        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns(existing);
        var request = MakeRequest() with { AboutImageUrl = "" };

        var result = await _handler.HandleAsync(request);

        result.AboutImageUrl.Should().Be("");
    }

    /// <summary>WorkingHours = null trong request (khi đã có bản ghi) phải giữ nguyên giá trị hiện tại.</summary>
    [Test]
    public async Task HandleAsync_ExistingInfo_NullWorkingHours_KeepsExistingWorkingHours()
    {
        var existing = Entity.Create("Cũ", "Mô tả cũ", 2000, "0281111111", "old@test.com", "Địa chỉ cũ",
            null, "8:00 - 17:00");
        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns(existing);
        var request = MakeRequest() with { WorkingHours = null };

        var result = await _handler.HandleAsync(request);

        result.WorkingHours.Should().Be("8:00 - 17:00");
    }

    /// <summary>WorkingHours = null khi chưa có bản ghi nào (tạo mới) phải mặc định thành chuỗi rỗng.</summary>
    [Test]
    public async Task HandleAsync_NoExistingInfo_NullWorkingHours_DefaultsToEmptyString()
    {
        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns((Entity?)null);
        var request = MakeRequest() with { WorkingHours = null };

        var result = await _handler.HandleAsync(request);

        result.WorkingHours.Should().Be("");
    }
}
