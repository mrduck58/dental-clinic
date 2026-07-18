using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Auth;

[TestFixture]
public class FillProfileHandlerTests
{
    private IUserRepository _userRepo = null!;
    private FillProfileHandler _handler = null!;

    private static readonly Guid TestUserId = Guid.NewGuid();

    private static readonly FillProfileCommand ValidCommand = new(
        UserId: TestUserId,
        FirstName: "An",
        LastName: "Nguyễn",
        FullName: null,
        PhoneNumber: "0901234567",
        DateOfBirth: new DateOnly(2000, 1, 15),
        Gender: "Male");

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _handler = new FillProfileHandler(_userRepo);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    /// <summary>
    /// Cập nhật hồ sơ thành công phải gọi UpdateAsync đúng 1 lần để lưu thay đổi vào database.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_CallsUpdateAsyncOnce()
    {
        var user = CreatePatientUser();
        _userRepo.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(ValidCommand);

        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// FullName phải được ghép theo thứ tự "LastName FirstName" (quy tắc tiếng Việt).
    /// Ví dụ: LastName="Nguyễn", FirstName="An" → FullName="Nguyễn An".
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_SetsFullNameAsLastNameSpaceFirstName()
    {
        var user = CreatePatientUser();
        _userRepo.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(ValidCommand);

        user.FullName.Should().Be("Nguyễn An");
    }

    /// <summary>
    /// Số điện thoại trong lệnh phải được cập nhật đúng vào hồ sơ người dùng.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_SetsPhoneNumberCorrectly()
    {
        var user = CreatePatientUser();
        _userRepo.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(ValidCommand);

        user.PhoneNumber.Should().Be(ValidCommand.PhoneNumber);
    }

    /// <summary>
    /// Ngày sinh và giới tính trong lệnh phải được cập nhật đúng vào hồ sơ người dùng.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_SetsDobAndGenderCorrectly()
    {
        var user = CreatePatientUser();
        _userRepo.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(ValidCommand);

        user.Patient?.DateOfBirth.Should().Be(ValidCommand.DateOfBirth);
        user.Gender.Should().Be(ValidCommand.Gender);
    }

    // ── Non-Patient (staff/dentist) branch ─────────────────────────────────────

    /// <summary>
    /// Với role khác "Patient" (vd: Dentist), handler phải cập nhật đầy đủ các field
    /// hồ sơ cá nhân bao gồm Address, Bio, Education, Specialty, YearsOfExperience —
    /// những field này bị bỏ qua ở nhánh Patient nhưng bắt buộc phải có ở nhánh nhân viên.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistRole_UpdatesPersonalProfileWithAllFields()
    {
        var user = User.Create("dr1", "dr1@test.com", "hash", "Dentist");
        _userRepo.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(user);

        var command = new FillProfileCommand(
            UserId: TestUserId,
            FirstName: "An",
            LastName: "Trần",
            FullName: null,
            PhoneNumber: "0909876543",
            DateOfBirth: new DateOnly(1990, 5, 20),
            Gender: "Female",
            Address: "123 Lê Lợi",
            ProfilePictureUrl: "http://img.test/pic.png",
            Bio: "Bác sĩ giỏi",
            Education: "Đại học Y Dược",
            Specialty: "Nha chu",
            YearsOfExperience: 8);

        await _handler.HandleAsync(command);

        user.FullName.Should().Be("Trần An");
        user.PhoneNumber.Should().Be("0909876543");
        user.Dentist?.Address.Should().Be("123 Lê Lợi");
        user.Dentist?.Biography.Should().Be("Bác sĩ giỏi");
        user.Dentist?.Education.Should().Be("Đại học Y Dược");
        user.Dentist?.Specialization.Should().Be("Nha chu");
        user.Dentist?.ExperienceYears.Should().Be(8);
    }

    /// <summary>
    /// Với role khác "Patient", nếu không có FullName/FirstName/LastName nào được cung cấp,
    /// handler phải giữ nguyên FullName cũ của user thay vì ghi đè thành rỗng.
    /// </summary>
    [Test]
    public async Task HandleAsync_StaffRole_NoNameProvided_FallsBackToExistingFullName()
    {
        var user = User.Create("staff1", "staff1@test.com", "hash", "Staff", fullName: "Tên Cũ");
        _userRepo.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(user);

        var command = new FillProfileCommand(
            UserId: TestUserId,
            FirstName: null,
            LastName: null,
            FullName: null,
            PhoneNumber: "0911111111",
            DateOfBirth: null,
            Gender: null);

        await _handler.HandleAsync(command);

        user.FullName.Should().Be("Tên Cũ");
    }

    // ── FullName precedence & blank-name edge cases ─────────────────────────────

    /// <summary>
    /// Khi command.FullName đã được cung cấp trực tiếp (không rỗng), handler phải dùng
    /// đúng giá trị đó, không được ghép lại từ LastName/FirstName.
    /// </summary>
    [Test]
    public async Task HandleAsync_PatientRole_FullNameProvidedDirectly_UsesFullNameAsIs()
    {
        var user = CreatePatientUser();
        _userRepo.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(user);

        var command = ValidCommand with { FullName = "Tên Trực Tiếp" };

        await _handler.HandleAsync(command);

        user.FullName.Should().Be("Tên Trực Tiếp");
    }

    /// <summary>
    /// Với role "Patient", nếu FullName, FirstName và LastName đều rỗng/null,
    /// FullName của user phải được set thành chuỗi rỗng (không fallback về giá trị cũ).
    /// </summary>
    [Test]
    public async Task HandleAsync_PatientRole_AllNameFieldsBlank_SetsFullNameToEmptyString()
    {
        var user = CreatePatientUser();
        _userRepo.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(user);

        var command = ValidCommand with { FullName = null, FirstName = null, LastName = null };

        await _handler.HandleAsync(command);

        user.FullName.Should().BeEmpty();
    }

    /// <summary>
    /// Với role "Patient", nếu PhoneNumber trong command là null, handler phải set
    /// PhoneNumber của user thành chuỗi rỗng thay vì giữ null.
    /// </summary>
    [Test]
    public async Task HandleAsync_PatientRole_NullPhoneNumber_DefaultsToEmptyString()
    {
        var user = CreatePatientUser();
        _userRepo.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(user);

        var command = ValidCommand with { PhoneNumber = null };

        await _handler.HandleAsync(command);

        user.PhoneNumber.Should().BeEmpty();
    }

    // ── Error paths ───────────────────────────────────────────────────────────

    /// <summary>
    /// UserId không tồn tại trong database phải ném NotFoundException.
    /// Trường hợp này xảy ra khi token hợp lệ nhưng user đã bị xóa khỏi hệ thống.
    /// </summary>
    [Test]
    public async Task HandleAsync_UserNotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.HandleAsync(ValidCommand);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Khi user không tồn tại, UpdateAsync không được gọi vì không có gì để cập nhật.
    /// </summary>
    [Test]
    public async Task HandleAsync_UserNotFound_DoesNotCallUpdateAsync()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Assert.CatchAsync(() => _handler.HandleAsync(ValidCommand));

        await _userRepo.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static User CreatePatientUser() =>
        User.Create("patient1", "patient@test.com", "hash", "Patient");
}
