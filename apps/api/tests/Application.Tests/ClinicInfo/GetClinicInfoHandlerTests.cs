using DentalClinic.API.Application.UseCases.ClinicInfo;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Entity = DentalClinic.API.Domain.Entities.ClinicInfo;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.ClinicInfo;

[TestFixture]
public class GetClinicInfoHandlerTests
{
    private IClinicInfoRepository _repo = null!;
    private GetClinicInfoHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IClinicInfoRepository>();
        _handler = new GetClinicInfoHandler(_repo);
    }

    /// <summary>Chưa seed thông tin phòng khám nào phải trả về null, không ném lỗi.</summary>
    [Test]
    public async Task HandleAsync_NoInfoSeeded_ReturnsNull()
    {
        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns((Entity?)null);

        var result = await _handler.HandleAsync();

        result.Should().BeNull();
    }

    /// <summary>Đã có thông tin phòng khám phải trả về đúng dữ liệu đã lưu.</summary>
    [Test]
    public async Task HandleAsync_InfoExists_ReturnsMappedDto()
    {
        var info = Entity.Create("Về chúng tôi", "Mô tả phòng khám", 2010, "0281234567",
            "clinic@test.com", "123 Đường ABC", null, "8:00 - 18:00");
        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns(info);

        var result = await _handler.HandleAsync();

        result.Should().NotBeNull();
        result!.AboutTitle.Should().Be("Về chúng tôi");
        result.FoundedYear.Should().Be(2010);
        result.Phone.Should().Be("0281234567");
    }

    /// <summary>
    /// Dữ liệu JSON bị hỏng (không hợp lệ) trong một danh sách phải được bỏ qua một cách an toàn,
    /// trả về danh sách rỗng cho trường đó thay vì ném lỗi, để không làm sập API.
    /// </summary>
    [Test]
    public async Task HandleAsync_CorruptedMilestonesJson_ReturnsEmptyListForThatFieldWithoutThrowing()
    {
        var info = Entity.Create("Về chúng tôi", "Mô tả phòng khám", 2010, "0281234567",
            "clinic@test.com", "123 Đường ABC", null, "8:00 - 18:00");
        info.SetCollections("{không phải json hợp lệ", null, null, null, null);
        _repo.GetAsync(Arg.Any<CancellationToken>()).Returns(info);

        var result = await _handler.HandleAsync();

        result.Should().NotBeNull();
        result!.Milestones.Should().BeEmpty();
    }
}
