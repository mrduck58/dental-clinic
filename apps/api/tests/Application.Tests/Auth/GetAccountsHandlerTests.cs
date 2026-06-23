using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Auth;

[TestFixture]
public class GetAccountsHandlerTests
{
    private IUserRepository _userRepo = null!;
    private GetAccountsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _handler = new GetAccountsHandler(_userRepo);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    /// <summary>
    /// Danh sách trả về chỉ chứa user có PasswordHash (đã được cấp tài khoản),
    /// nhân viên chưa có tài khoản phải bị lọc ra.
    /// </summary>
    [Test]
    public async Task HandleAsync_MixedUsers_ReturnsOnlyUsersWithAccount()
    {
        var withAccount1 = User.Create("u1", "a@test.com", "hash1", "Admin");
        var withAccount2 = User.Create("u2", "b@test.com", "hash2", "Staff");
        var withoutAccount = User.CreateEmployee("c@test.com", "Dentist");

        _userRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { withAccount1, withAccount2, withoutAccount });

        var result = await _handler.HandleAsync();

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Nếu toàn bộ user trong DB đều là nhân viên chưa có tài khoản, kết quả phải rỗng —
    /// không nên trả về danh sách với PasswordHash null.
    /// </summary>
    [Test]
    public async Task HandleAsync_AllWithoutAccount_ReturnsEmpty()
    {
        var employees = new List<User>
        {
            User.CreateEmployee("a@test.com", "Staff"),
            User.CreateEmployee("b@test.com", "Dentist"),
        };
        _userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(employees);

        var result = await _handler.HandleAsync();

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Repository rỗng hoàn toàn phải trả về danh sách rỗng, không throw exception.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptyRepo_ReturnsEmpty()
    {
        _userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<User>());

        var result = await _handler.HandleAsync();

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tất cả các field của AccountDto phải được ánh xạ đúng từ User entity,
    /// đảm bảo caller nhận được thông tin đầy đủ để hiển thị danh sách tài khoản.
    /// </summary>
    [Test]
    public async Task HandleAsync_MapsFieldsCorrectly()
    {
        var user = User.Create("adminuser", "admin@test.com", "hash", "Admin", "0901234567", "Admin Full");
        _userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<User> { user });

        var result = (await _handler.HandleAsync()).Single();

        result.Username.Should().Be("adminuser");
        result.Email.Should().Be("admin@test.com");
        result.Role.Should().Be("Admin");
        result.FullName.Should().Be("Admin Full");
        result.PhoneNumber.Should().Be("0901234567");
        result.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Khi tất cả user đều có tài khoản, toàn bộ danh sách phải được trả về
    /// mà không bị lọc bớt.
    /// </summary>
    [Test]
    public async Task HandleAsync_AllWithAccount_ReturnsAll()
    {
        var users = new List<User>
        {
            User.Create("u1", "a@test.com", "h1", "Staff"),
            User.Create("u2", "b@test.com", "h2", "Dentist"),
            User.Create("u3", "c@test.com", "h3", "Admin"),
        };
        _userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(users);

        var result = await _handler.HandleAsync();

        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Handler phải gọi GetAllAsync đúng 1 lần — không được gọi nhiều lần
    /// gây ra nhiều query không cần thiết vào database.
    /// </summary>
    [Test]
    public async Task HandleAsync_CallsGetAllAsyncOnce()
    {
        _userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<User>());

        await _handler.HandleAsync();

        await _userRepo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }
}
