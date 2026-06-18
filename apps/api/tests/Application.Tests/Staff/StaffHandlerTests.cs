using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Staff;

[TestFixture]
public class StaffHandlerTests
{
    private IUserRepository _userRepo = null!;
    private IEmailService _emailService = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _emailService = Substitute.For<IEmailService>();

        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateStaffHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tạo hồ sơ nhân viên mới với email chưa tồn tại phải gọi AddAsync 1 lần,
    /// lưu employee chưa có tài khoản vào database.
    /// </summary>
    [Test]
    public async Task CreateStaff_NewEmail_CallsAddAsyncOnce()
    {
        var handler = new CreateStaffHandler(_userRepo);

        await handler.HandleAsync(BuildCreateStaffCommand());

        await _userRepo.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Nhân viên mới tạo qua CreateStaffHandler không được có tài khoản đăng nhập,
    /// tài khoản chỉ được cấp riêng qua CreateStaffAccountHandler sau này.
    /// </summary>
    [Test]
    public async Task CreateStaff_NewEmail_ReturnedDtoHasNoAccount()
    {
        var handler = new CreateStaffHandler(_userRepo);

        var result = await handler.HandleAsync(BuildCreateStaffCommand());

        result.HasAccount.Should().BeFalse();
    }

    /// <summary>
    /// Email đã được dùng bởi nhân viên/tài khoản khác phải ném ConflictException,
    /// ngăn tạo hồ sơ trùng email.
    /// </summary>
    [Test]
    public async Task CreateStaff_DuplicateEmail_ThrowsConflictException()
    {
        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateStaffHandler(_userRepo);

        Func<Task> act = () => handler.HandleAsync(BuildCreateStaffCommand());

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>
    /// Khi email trùng, AddAsync không được gọi để tránh lưu dữ liệu trùng lặp.
    /// </summary>
    [Test]
    public async Task CreateStaff_DuplicateEmail_DoesNotCallAddAsync()
    {
        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateStaffHandler(_userRepo);

        Assert.CatchAsync(() => handler.HandleAsync(BuildCreateStaffCommand()));

        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdateStaffHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cập nhật thông tin nhân viên tồn tại phải gọi UpdateAsync 1 lần và trả về DTO mới.
    /// </summary>
    [Test]
    public async Task UpdateStaff_ExistingStaff_CallsUpdateAsyncOnce()
    {
        var user = MakeEmployee();
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new UpdateStaffHandler(_userRepo);

        await handler.HandleAsync(BuildUpdateCommand(user.Id, email: user.Email));

        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Nhân viên không tồn tại phải ném NotFoundException, không gọi UpdateAsync.
    /// </summary>
    [Test]
    public async Task UpdateStaff_NotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new UpdateStaffHandler(_userRepo);

        Func<Task> act = () => handler.HandleAsync(BuildUpdateCommand(Guid.NewGuid()));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Đổi sang email mới đã tồn tại trong hệ thống phải ném ConflictException.
    /// </summary>
    [Test]
    public async Task UpdateStaff_NewEmailAlreadyTaken_ThrowsConflictException()
    {
        var user = MakeEmployee(email: "old@test.com");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _userRepo.ExistsByEmailAsync("new@test.com", Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateStaffHandler(_userRepo);

        Func<Task> act = () => handler.HandleAsync(BuildUpdateCommand(user.Id, email: "new@test.com"));

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>
    /// Giữ nguyên email cũ khi update không được kiểm tra conflict,
    /// tránh lỗi khi email của chính nhân viên đó bị coi là "đã tồn tại".
    /// </summary>
    [Test]
    public async Task UpdateStaff_SameEmail_DoesNotCheckEmailConflict()
    {
        var user = MakeEmployee(email: "same@test.com");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new UpdateStaffHandler(_userRepo);

        await handler.HandleAsync(BuildUpdateCommand(user.Id, email: "same@test.com"));

        await _userRepo.DidNotReceive().ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ResetStaffPasswordHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reset mật khẩu nhân viên tồn tại phải gọi UpdateAsync và gửi email
    /// với mật khẩu tạm thời mới để nhân viên đăng nhập lại.
    /// </summary>
    [Test]
    public async Task ResetPassword_ExistingUser_UpdatesAndSendsEmail()
    {
        var user = MakeEmployee();
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new ResetStaffPasswordHandler(_userRepo, _emailService);

        await handler.HandleAsync(user.Id);

        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendStaffCredentialsAsync(
            user.Email, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Nhân viên không tồn tại phải ném NotFoundException, không gửi email.
    /// </summary>
    [Test]
    public async Task ResetPassword_NotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new ResetStaffPasswordHandler(_userRepo, _emailService);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Mật khẩu tạm thời trả về phải có độ dài 8 ký tự đúng theo cấu hình,
    /// để admin có thể thông báo lại cho nhân viên nếu cần.
    /// </summary>
    [Test]
    public async Task ResetPassword_ExistingUser_ReturnsTemporaryPassword8Chars()
    {
        var user = MakeEmployee();
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new ResetStaffPasswordHandler(_userRepo, _emailService);

        var result = await handler.HandleAsync(user.Id);

        result.TemporaryPassword.Should().HaveLength(8);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateStaffAccountHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cấp tài khoản cho nhân viên chưa có account phải gọi UpdateAsync và gửi email thông tin đăng nhập.
    /// </summary>
    [Test]
    public async Task CreateStaffAccount_EmployeeWithoutAccount_UpdatesAndSendsEmail()
    {
        var user = User.CreateEmployee("emp@test.com", "Staff");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new CreateStaffAccountHandler(_userRepo, _emailService);

        await handler.HandleAsync(user.Id);

        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendStaffCredentialsAsync(
            user.Email, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Nhân viên không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task CreateStaffAccount_NotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new CreateStaffAccountHandler(_userRepo, _emailService);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Nhân viên đã có tài khoản rồi không được cấp lại, phải ném ConflictException.
    /// </summary>
    [Test]
    public async Task CreateStaffAccount_AlreadyHasAccount_ThrowsConflictException()
    {
        var user = User.Create("user1", "emp@test.com", "hash", "Staff");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new CreateStaffAccountHandler(_userRepo, _emailService);

        Func<Task> act = () => handler.HandleAsync(user.Id);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*đã có tài khoản*");
    }

    /// <summary>
    /// Sau khi cấp tài khoản, nhân viên phải có HasAccount = true trong kết quả trả về.
    /// </summary>
    [Test]
    public async Task CreateStaffAccount_Success_ReturnedDtoHasAccount()
    {
        var user = User.CreateEmployee("emp@test.com", "Staff");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new CreateStaffAccountHandler(_userRepo, _emailService);

        var result = await handler.HandleAsync(user.Id);

        result.HasAccount.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetStaffHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Query page &lt; 1 phải được clamp về 1 để tránh lỗi SQL offset âm.
    /// </summary>
    [Test]
    public async Task GetStaff_PageLessThanOne_ClampsToOne()
    {
        _userRepo.GetStaffPagedAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<User>().AsReadOnly(), 0));
        _userRepo.GetStaffStatsAsync(Arg.Any<CancellationToken>())
            .Returns(new StaffStatsResult(0, 0, 0));
        var handler = new GetStaffHandler(_userRepo);

        var result = await handler.HandleAsync(new GetStaffQuery(null, null, null, Page: 0, PageSize: 10));

        result.Page.Should().Be(1);
    }

    /// <summary>
    /// PageSize lớn hơn 100 phải được clamp về 100, tránh query trả về quá nhiều dòng.
    /// </summary>
    [Test]
    public async Task GetStaff_PageSizeOver100_ClampedTo100()
    {
        _userRepo.GetStaffPagedAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<User>().AsReadOnly(), 0));
        _userRepo.GetStaffStatsAsync(Arg.Any<CancellationToken>())
            .Returns(new StaffStatsResult(0, 0, 0));
        var handler = new GetStaffHandler(_userRepo);

        await handler.HandleAsync(new GetStaffQuery(null, null, null, Page: 1, PageSize: 999));

        await _userRepo.Received(1).GetStaffPagedAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            1, 100, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TotalCount và Items phải được lấy đúng từ kết quả paged query của repository.
    /// </summary>
    [Test]
    public async Task GetStaff_ValidQuery_ReturnsTotalCountAndItems()
    {
        var users = new List<User>
        {
            User.Create("u1", "a@test.com", "h", "Staff"),
            User.Create("u2", "b@test.com", "h", "Dentist"),
        };
        _userRepo.GetStaffPagedAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((users.AsReadOnly(), 20));
        _userRepo.GetStaffStatsAsync(Arg.Any<CancellationToken>())
            .Returns(new StaffStatsResult(20, 5, 3));
        var handler = new GetStaffHandler(_userRepo);

        var result = await handler.HandleAsync(new GetStaffQuery(null, null, null, 1, 10));

        result.TotalCount.Should().Be(20);
        result.Items.Should().HaveCount(2);
    }

    /// <summary>
    /// Statistics trong kết quả phải ánh xạ đúng từ StaffStatsResult của repository.
    /// </summary>
    [Test]
    public async Task GetStaff_ReturnsStatsFromRepo()
    {
        _userRepo.GetStaffPagedAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<User>().AsReadOnly(), 0));
        _userRepo.GetStaffStatsAsync(Arg.Any<CancellationToken>())
            .Returns(new StaffStatsResult(10, 3, 2));
        var handler = new GetStaffHandler(_userRepo);

        var result = await handler.HandleAsync(new GetStaffQuery(null, null, null, 1, 10));

        result.Statistics.TotalEmployees.Should().Be(10);
        result.Statistics.TotalDentists.Should().Be(3);
        result.Statistics.TotalDoctors.Should().Be(2);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static User MakeEmployee(string email = "emp@test.com")
        => User.CreateEmployee(email, "Staff", "0901234567", "Nhân Viên Test");

    private static CreateStaffCommand BuildCreateStaffCommand(string email = "newstaff@test.com")
        => new(
            FullName: "Nhân Viên Mới",
            Email: email,
            PhoneNumber: "0901234567",
            Role: "Staff",
            EmployeeId: null, Department: null, EmploymentStatus: null,
            ProfilePictureUrl: null, ProfessionalNotes: null,
            Specialty: null, LicenseNumber: null, YearsOfExperience: null,
            Gender: null, DateOfBirth: null, Address: null, StartDate: null,
            ServicesHandled: null, CertificateIssuedDate: null, CertificateIssuedBy: null,
            Education: null, Bio: null, Position: null);

    private static UpdateStaffCommand BuildUpdateCommand(Guid id, string email = "emp@test.com")
        => new(
            Id: id,
            FullName: "Tên Mới", Email: email, PhoneNumber: "0901234567",
            Role: "Staff", Department: null, EmploymentStatus: null,
            ProfilePictureUrl: null, ProfessionalNotes: null, IsActive: true,
            Specialty: null, LicenseNumber: null, YearsOfExperience: null,
            Gender: null, DateOfBirth: null, Address: null, StartDate: null,
            ServicesHandled: null, CertificateIssuedDate: null, CertificateIssuedBy: null,
            Education: null, Bio: null, Position: null);
}
