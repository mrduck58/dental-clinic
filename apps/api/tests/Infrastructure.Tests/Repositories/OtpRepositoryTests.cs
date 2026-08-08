using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Repositories;

[TestFixture]
public class OtpRepositoryTests
{
    private AppDbContext _db = null!;
    private OtpRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new OtpRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    /// <summary>AddAsync phải lưu mã OTP vào DB.</summary>
    [Test]
    public async Task AddAsync_ValidOtp_PersistsToDatabase()
    {
        await _sut.AddAsync(OtpCode.Create("a@test.com"));

        (await _db.OtpCodes.CountAsync()).Should().Be(1);
    }

    /// <summary>Còn hiệu lực, chưa dùng, đúng email/purpose phải được trả về.</summary>
    [Test]
    public async Task GetLatestValidAsync_ValidUnusedOtp_ReturnsIt()
    {
        var otp = OtpCode.Create("a@test.com", OtpPurpose.Registration);
        _db.OtpCodes.Add(otp);
        await _db.SaveChangesAsync();

        var result = await _sut.GetLatestValidAsync("a@test.com", OtpPurpose.Registration);

        result.Should().NotBeNull();
        result!.Id.Should().Be(otp.Id);
    }

    /// <summary>Mã đã dùng rồi phải không được trả về nữa.</summary>
    [Test]
    public async Task GetLatestValidAsync_UsedOtp_ReturnsNull()
    {
        var otp = OtpCode.Create("b@test.com", OtpPurpose.Registration);
        otp.MarkUsed();
        _db.OtpCodes.Add(otp);
        await _db.SaveChangesAsync();

        var result = await _sut.GetLatestValidAsync("b@test.com", OtpPurpose.Registration);

        result.Should().BeNull();
    }

    /// <summary>Mã đã hết hạn phải không được trả về nữa.</summary>
    [Test]
    public async Task GetLatestValidAsync_ExpiredOtp_ReturnsNull()
    {
        var otp = OtpCode.Create("c@test.com", OtpPurpose.Registration, expiryMinutes: -1);
        _db.OtpCodes.Add(otp);
        await _db.SaveChangesAsync();

        var result = await _sut.GetLatestValidAsync("c@test.com", OtpPurpose.Registration);

        result.Should().BeNull();
    }

    /// <summary>Sai purpose (ví dụ ResetPassword thay vì Registration) phải không được trả về.</summary>
    [Test]
    public async Task GetLatestValidAsync_WrongPurpose_ReturnsNull()
    {
        var otp = OtpCode.Create("d@test.com", OtpPurpose.Registration);
        _db.OtpCodes.Add(otp);
        await _db.SaveChangesAsync();

        var result = await _sut.GetLatestValidAsync("d@test.com", OtpPurpose.PasswordReset);

        result.Should().BeNull();
    }

    /// <summary>Có nhiều mã hợp lệ cho cùng email/purpose thì phải trả về mã tạo mới nhất.</summary>
    [Test]
    public async Task GetLatestValidAsync_MultipleValidCodes_ReturnsMostRecent()
    {
        var older = OtpCode.Create("e@test.com", OtpPurpose.Registration);
        await Task.Delay(10);
        var newer = OtpCode.Create("e@test.com", OtpPurpose.Registration);
        _db.OtpCodes.AddRange(older, newer);
        await _db.SaveChangesAsync();

        var result = await _sut.GetLatestValidAsync("e@test.com", OtpPurpose.Registration);

        result!.Id.Should().Be(newer.Id);
    }

    /// <summary>InvalidateAllAsync phải đánh dấu đã dùng cho toàn bộ mã đang chờ của email/purpose đó.</summary>
    [Test]
    public async Task InvalidateAllAsync_PendingCodes_MarksAllAsUsed()
    {
        var first = OtpCode.Create("f@test.com", OtpPurpose.Registration);
        var second = OtpCode.Create("f@test.com", OtpPurpose.Registration);
        _db.OtpCodes.AddRange(first, second);
        await _db.SaveChangesAsync();

        await _sut.InvalidateAllAsync("f@test.com", OtpPurpose.Registration);

        (await _db.OtpCodes.Where(o => o.Email == "f@test.com").AllAsync(o => o.IsUsed)).Should().BeTrue();
    }

    /// <summary>InvalidateAllAsync không được ảnh hưởng tới mã của email hoặc purpose khác.</summary>
    [Test]
    public async Task InvalidateAllAsync_DoesNotAffectOtherEmailOrPurpose()
    {
        var target = OtpCode.Create("g@test.com", OtpPurpose.Registration);
        var otherEmail = OtpCode.Create("h@test.com", OtpPurpose.Registration);
        var otherPurpose = OtpCode.Create("g@test.com", OtpPurpose.PasswordReset);
        _db.OtpCodes.AddRange(target, otherEmail, otherPurpose);
        await _db.SaveChangesAsync();

        await _sut.InvalidateAllAsync("g@test.com", OtpPurpose.Registration);

        (await _db.OtpCodes.FindAsync(otherEmail.Id))!.IsUsed.Should().BeFalse();
        (await _db.OtpCodes.FindAsync(otherPurpose.Id))!.IsUsed.Should().BeFalse();
    }

    /// <summary>UpdateAsync phải lưu lại trạng thái đã dùng.</summary>
    [Test]
    public async Task UpdateAsync_MarkedUsed_PersistsChange()
    {
        var otp = OtpCode.Create("i@test.com");
        _db.OtpCodes.Add(otp);
        await _db.SaveChangesAsync();

        otp.MarkUsed();
        await _sut.UpdateAsync(otp);

        var reloaded = await _db.OtpCodes.FindAsync(otp.Id);
        reloaded!.IsUsed.Should().BeTrue();
    }
}
