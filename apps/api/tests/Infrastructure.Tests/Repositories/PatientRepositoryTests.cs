using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Repositories;

[TestFixture]
public class PatientRepositoryTests
{
    private AppDbContext _db = null!;
    private PatientRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new PatientRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<Patient> SeedWalkInPatientAsync(string fullName, string phone)
    {
        var user = User.CreateEmployee($"walkin-{Guid.NewGuid()}@songiangdental.com", UserRole.Patient, phoneNumber: phone, fullName: fullName);
        _db.Users.Add(user);
        var patient = Patient.Create(user.Id, new DateOnly(1990, 1, 1), "Nam", phoneNumber: phone);
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return patient;
    }

    // ── SearchAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task SearchAsync_MatchesPartOfFullName_CaseInsensitively()
    {
        await SeedWalkInPatientAsync("Nguyễn Văn Hùng", "0901111111");
        await SeedWalkInPatientAsync("Trần Thị Lan", "0902222222");

        var results = await _sut.SearchAsync("hùng", 10);

        results.Should().ContainSingle().Which.FullName.Should().Be("Nguyễn Văn Hùng");
    }

    /// <summary>
    /// Bệnh nhân tạo tại quầy không có tài khoản — số điện thoại chỉ nằm ở Patient.PhoneNumber,
    /// nên tra cứu phải dò được cột này.
    /// </summary>
    [Test]
    public async Task SearchAsync_MatchesPhoneOfAccountlessPatient()
    {
        await SeedWalkInPatientAsync("Trần Thị Lan", "0902222222");

        var results = await _sut.SearchAsync("22222", 10);

        results.Should().ContainSingle().Which.FullName.Should().Be("Trần Thị Lan");
    }

    /// <summary>Bệnh nhân có tài khoản: số điện thoại lưu ở User, tra cứu vẫn phải khớp.</summary>
    [Test]
    public async Task SearchAsync_MatchesPhoneOfLinkedUserAccount()
    {
        var user = User.Create("u1", "u1@test.com", "hash", UserRole.Patient,
            phoneNumber: "0903333333", fullName: "Lê Minh Quân");
        _db.Users.Add(user);
        var patient = Patient.Create(user.Id, new DateOnly(1992, 3, 3), "Nam");
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var results = await _sut.SearchAsync("0903333333", 10);

        results.Should().ContainSingle().Which.FullName.Should().Be("Lê Minh Quân");
    }

    [Test]
    public async Task SearchAsync_RespectsLimit()
    {
        for (var i = 0; i < 5; i++)
            await SeedWalkInPatientAsync($"Nguyễn Test {i}", $"090000000{i}");

        var results = await _sut.SearchAsync("nguyễn", 3);

        results.Should().HaveCount(3);
    }

    [Test]
    public async Task SearchAsync_NoMatch_ReturnsEmpty()
    {
        await SeedWalkInPatientAsync("Nguyễn Văn Hùng", "0901111111");

        var results = await _sut.SearchAsync("khong-ton-tai", 10);

        results.Should().BeEmpty();
    }
}
