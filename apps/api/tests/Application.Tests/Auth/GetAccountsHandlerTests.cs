using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
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
        var withAccount1 = User.Create("u1", "a@test.com", "hash1", UserRole.Admin);
        var withAccount2 = User.Create("u2", "b@test.com", "hash2", UserRole.Staff);
        var withoutAccount = User.CreateEmployee("c@test.com", UserRole.Dentist);

        _userRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<User> { withAccount1, withAccount2, withoutAccount });

        var result = await _handler.Handle(new GetAccountsQuery(), CancellationToken.None);

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
            User.CreateEmployee("a@test.com", UserRole.Staff),
            User.CreateEmployee("b@test.com", UserRole.Dentist),
        };
        _userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(employees);

        var result = await _handler.Handle(new GetAccountsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Repository rỗng hoàn toàn phải trả về danh sách rỗng, không throw exception.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptyRepo_ReturnsEmpty()
    {
        _userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<User>());

        var result = await _handler.Handle(new GetAccountsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tất cả các field của AccountDto phải được ánh xạ đúng từ User entity,
    /// đảm bảo caller nhận được thông tin đầy đủ để hiển thị danh sách tài khoản.
    /// </summary>
    [Test]
    public async Task HandleAsync_MapsFieldsCorrectly()
    {
        var user = User.Create("adminuser", "admin@test.com", "hash", UserRole.Admin, "0901234567", "Admin Full");
        _userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<User> { user });

        var result = (await _handler.Handle(new GetAccountsQuery(), CancellationToken.None)).Single();

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
            User.Create("u1", "a@test.com", "h1", UserRole.Staff),
            User.Create("u2", "b@test.com", "h2", UserRole.Dentist),
            User.Create("u3", "c@test.com", "h3", UserRole.Admin),
        };
        _userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(users);

        var result = await _handler.Handle(new GetAccountsQuery(), CancellationToken.None);

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

        await _handler.Handle(new GetAccountsQuery(), CancellationToken.None);

        await _userRepo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Id và CreatedAt của user cũng phải được ánh xạ đúng sang AccountDto,
    /// đảm bảo client có đủ thông tin định danh và thời điểm tạo tài khoản.
    /// </summary>
    [Test]
    public async Task HandleAsync_MapsIdAndCreatedAtCorrectly()
    {
        var user = User.Create("adminuser", "admin@test.com", "hash", UserRole.Admin);
        _userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<User> { user });

        var result = (await _handler.Handle(new GetAccountsQuery(), CancellationToken.None)).Single();

        result.Id.Should().Be(user.Id);
        result.CreatedAt.Should().Be(user.CreatedAt);
    }

    /// <summary>
    /// User bị vô hiệu hóa (IsActive = false) phải được ánh xạ đúng giá trị IsActive = false,
    /// không được mặc định thành true trong AccountDto.
    /// </summary>
    [Test]
    public async Task HandleAsync_InactiveUser_MapsIsActiveFalse()
    {
        var user = User.Create("inactiveuser", "inactive@test.com", "hash", UserRole.Staff);
        user.SetActive(false);
        _userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<User> { user });

        var result = (await _handler.Handle(new GetAccountsQuery(), CancellationToken.None)).Single();

        result.IsActive.Should().BeFalse();
    }
}
