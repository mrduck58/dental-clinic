using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Staff;

[TestFixture]
public class GetDentistsHandlerTests
{
    private IUserRepository _userRepo = null!;
    private GetDentistsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _userRepo.GetStaffPagedAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<User>().AsReadOnly(), 0));
        _handler = new GetDentistsHandler(_userRepo);
    }

    // ── Query behavior ────────────────────────────────────────────────────────

    /// <summary>
    /// Handler lấy toàn bộ nhân sự (status=null, pageSize=500) rồi lọc bằng LINQ trong bộ nhớ,
    /// vì DB có thể lưu role/status với cách viết hoa khác nhau ("Dentist"/"doctor"/"Active").
    /// </summary>
    [Test]
    public async Task HandleAsync_CallsRepoWithCorrectParameters()
    {
        await _handler.HandleAsync();

        await _userRepo.Received(1).GetStaffPagedAsync(
            null, null, null, 1, 500, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi repo không có dữ liệu, kết quả phải là danh sách rỗng — không được throw exception.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptyRepo_ReturnsEmptyList()
    {
        var result = await _handler.HandleAsync();

        result.Should().BeEmpty();
    }

    // ── Filtering ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Trong danh sách hỗn hợp, chỉ user có role "Dentist" hoặc "Doctor" được trả về,
    /// các role khác (Staff, Admin) bị loại bỏ.
    /// </summary>
    [Test]
    public async Task HandleAsync_MixedRoles_ReturnsDentistsAndDoctorsOnly()
    {
        var dentist = User.Create("d1", "dentist@test.com", "hash", "Dentist");
        var doctor = User.Create("d2", "doctor@test.com", "hash", "Doctor");
        var staff = User.Create("s1", "staff@test.com", "hash", "Staff");
        var admin = User.Create("a1", "admin@test.com", "hash", "Admin");

        SetRepoData(dentist, doctor, staff, admin);

        var result = (await _handler.HandleAsync()).ToList();

        result.Should().HaveCount(2);
        result.Select(r => r.Id).Should().BeEquivalentTo(new[] { dentist.Id, doctor.Id });
    }

    /// <summary>
    /// User có role "Staff" không được xuất hiện trong danh sách bác sĩ.
    /// </summary>
    [Test]
    public async Task HandleAsync_OnlyStaffInRepo_ReturnsEmptyList()
    {
        SetRepoData(User.Create("s1", "staff@test.com", "hash", "Staff"));

        var result = await _handler.HandleAsync();

        result.Should().BeEmpty();
    }

    /// <summary>
    /// User có role "Admin" không được xuất hiện trong danh sách bác sĩ.
    /// </summary>
    [Test]
    public async Task HandleAsync_OnlyAdminInRepo_ReturnsEmptyList()
    {
        SetRepoData(User.Create("a1", "admin@test.com", "hash", "Admin"));

        var result = await _handler.HandleAsync();

        result.Should().BeEmpty();
    }

    // ── DTO mapping ───────────────────────────────────────────────────────────

    /// <summary>
    /// DentistSummaryDto phải ánh xạ đúng Id và FullName từ User entity.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistUser_MapsIdAndFullNameCorrectly()
    {
        var dentist = User.Create("bs1", "bs@test.com", "hash", "Dentist",
            fullName: "Bác sĩ Nguyễn Văn A");
        SetRepoData(dentist);

        var result = (await _handler.HandleAsync()).Single();

        result.Id.Should().Be(dentist.Id);
        result.FullName.Should().Be("Bác sĩ Nguyễn Văn A");
    }

    /// <summary>
    /// Nhiều bác sĩ trong repo phải được ánh xạ đầy đủ theo thứ tự.
    /// </summary>
    [Test]
    public async Task HandleAsync_MultipleDentists_ReturnsAllMapped()
    {
        var d1 = User.Create("d1", "d1@test.com", "hash", "Dentist", fullName: "Bác sĩ A");
        var d2 = User.Create("d2", "d2@test.com", "hash", "Doctor", fullName: "Bác sĩ B");
        SetRepoData(d1, d2);

        var result = (await _handler.HandleAsync()).ToList();

        result.Should().HaveCount(2);
        result[0].FullName.Should().Be("Bác sĩ A");
        result[1].FullName.Should().Be("Bác sĩ B");
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private void SetRepoData(params User[] users)
    {
        _userRepo.GetStaffPagedAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((users.ToList().AsReadOnly(), users.Length));
    }
}
