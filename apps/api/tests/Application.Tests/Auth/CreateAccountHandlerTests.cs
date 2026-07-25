using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Auth;

[TestFixture]
public class CreateAccountHandlerTests
{
    private IUserRepository _userRepo = null!;
    private IEmailService _emailService = null!;
    private IActivityLogService _activityLog = null!;
    private ICurrentUserService _currentUser = null!;
    private CreateAccountHandler _handler = null!;

    private static readonly CreateAccountCommand ValidCommand = new(
        FullName: "Nguyễn Văn A",
        Email: "nguyenvana@test.com",
        PhoneNumber: "0901234567",
        Role: "Staff");

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _emailService = Substitute.For<IEmailService>();
        _activityLog = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _handler = new CreateAccountHandler(_userRepo, _emailService, _activityLog, _currentUser);

        // Mặc định: email và username chưa tồn tại
        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    /// <summary>
    /// Tạo tài khoản với email mới phải gọi AddAsync đúng 1 lần để lưu user vào database,
    /// không được bỏ sót hoặc gọi thừa làm tạo user trùng lặp.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_CallsAddAsyncOnce()
    {
        await _handler.HandleAsync(ValidCommand);

        await _userRepo.Received(1).AddAsync(Arg.Any<Domain.Entities.User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sau khi tạo tài khoản thành công, phải gửi email chứa thông tin đăng nhập
    /// (username và mật khẩu tạm thời) đến đúng địa chỉ email của nhân viên.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_SendsCredentialEmailOnce()
    {
        await _handler.HandleAsync(ValidCommand);

        await _emailService.Received(1).SendStaffCredentialsAsync(
            ValidCommand.Email,
            ValidCommand.FullName,
            Arg.Any<string>(),   // password là random, không kiểm tra giá trị cụ thể
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Response trả về phải chứa đúng email và role của tài khoản vừa tạo,
    /// để caller xác nhận thông tin trước khi hiển thị cho admin.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_ReturnsCorrectEmailAndRole()
    {
        var result = await _handler.HandleAsync(ValidCommand);

        result.Email.Should().Be(ValidCommand.Email);
        result.Role.Should().Be(ValidCommand.Role);
    }

    /// <summary>
    /// Username được tự động sinh từ phần trước @ của email (email prefix),
    /// giúp username có ý nghĩa và dễ nhận biết thay vì chuỗi ngẫu nhiên.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_UsernameDerivedFromEmailPrefix()
    {
        // "nguyenvana@test.com" → "nguyenvana"
        var result = await _handler.HandleAsync(ValidCommand);

        result.Username.Should().Be("nguyenvana");
    }

    /// <summary>
    /// Dấu chấm trong email prefix phải được chuyển thành dấu gạch dưới trong username,
    /// vì username không được chứa dấu chấm theo quy tắc định danh hệ thống.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmailWithDots_UsernameReplacesDotWithUnderscore()
    {
        var cmd = ValidCommand with { Email = "nguyen.van.a@test.com" };

        var result = await _handler.HandleAsync(cmd);

        result.Username.Should().Be("nguyen_van_a");
    }

    /// <summary>
    /// Username bị trùng không được làm dừng việc tạo tài khoản — handler tự thêm suffix
    /// ngẫu nhiên và vẫn gọi AddAsync 1 lần để lưu user với username mới.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateUsername_StillCallsAddAsyncOnce()
    {
        _userRepo.ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        await _handler.HandleAsync(ValidCommand);

        await _userRepo.Received(1).AddAsync(Arg.Any<Domain.Entities.User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi username gốc đã tồn tại, handler phải thêm suffix hex vào cuối để tạo username duy nhất,
    /// đảm bảo username luôn bắt đầu bằng prefix gốc để còn nhận ra được.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateUsername_UsernameSuffixed()
    {
        _userRepo.ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.HandleAsync(ValidCommand);

        // suffix có dạng "nguyenvana_xxxx" (4 ký tự hex ngẫu nhiên)
        result.Username.Should().StartWith("nguyenvana_");
        result.Username.Length.Should().BeGreaterThan("nguyenvana".Length);
    }

    /// <summary>
    /// Dấu gạch ngang trong email prefix cũng phải được chuyển thành dấu gạch dưới
    /// giống như dấu chấm, đảm bảo username hợp lệ theo quy tắc định danh hệ thống.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmailWithHyphen_UsernameReplacesHyphenWithUnderscore()
    {
        var cmd = ValidCommand with { Email = "nguyen-van-a@test.com" };

        var result = await _handler.HandleAsync(cmd);

        result.Username.Should().Be("nguyen_van_a");
    }

    /// <summary>
    /// Tạo tài khoản thành công phải ghi log hoạt động đúng 1 lần với action Create,
    /// module Account và trạng thái Success, để admin có thể tra cứu lịch sử thao tác.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_LogsCreateActivityOnce()
    {
        await _handler.HandleAsync(ValidCommand);

        await _activityLog.Received(1).LogAsync(
            Arg.Any<Guid?>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            ActivityAction.Create,
            ActivityModule.Account,
            Arg.Any<string>(),
            ActivityStatus.Success,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    // ── Error paths ───────────────────────────────────────────────────────────

    /// <summary>
    /// Email đã tồn tại trong hệ thống phải ném ConflictException với message chứa email,
    /// để admin biết chính xác email nào đang bị trùng.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateEmail_ThrowsConflictException()
    {
        _userRepo.ExistsByEmailAsync(ValidCommand.Email, Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = () => _handler.HandleAsync(ValidCommand);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage($"*{ValidCommand.Email}*");
    }

    /// <summary>
    /// Khi email trùng, handler phải dừng ngay và không gọi AddAsync,
    /// tránh tạo ra user trùng lặp trong database trước khi exception được ném.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateEmail_DoesNotCallAddAsync()
    {
        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        Assert.CatchAsync(() => _handler.HandleAsync(ValidCommand));

        await _userRepo.DidNotReceive().AddAsync(Arg.Any<Domain.Entities.User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi email trùng, tuyệt đối không được gửi email thông tin đăng nhập,
    /// vì user chưa được tạo và việc gửi email sẽ gây nhầm lẫn cho người nhận.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateEmail_DoesNotSendEmail()
    {
        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        Assert.CatchAsync(() => _handler.HandleAsync(ValidCommand));

        await _emailService.DidNotReceive().SendStaffCredentialsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
