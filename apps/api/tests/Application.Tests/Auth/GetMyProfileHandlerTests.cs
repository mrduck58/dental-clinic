using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Auth;

[TestFixture]
public class GetMyProfileHandlerTests
{
    private IUserRepository _userRepo = null!;
    private GetMyProfileHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _handler = new GetMyProfileHandler(_userRepo);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    /// <summary>
    /// Tài khoản tồn tại phải trả về UserProfileDto với email khớp đúng,
    /// để client hiển thị thông tin cá nhân người dùng đang đăng nhập.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_ReturnsCorrectEmail()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("patient1", "patient@test.com", "hash", "Patient", "0901234567");
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        result.Email.Should().Be("patient@test.com");
    }

    /// <summary>
    /// Số điện thoại được truyền vào User.Create phải xuất hiện đúng trong DTO trả về,
    /// để bệnh nhân thấy thông tin liên lạc của mình trên màn hình hồ sơ.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_ReturnsCorrectPhoneNumber()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("patient2", "patient2@test.com", "hash", "Patient", "0912345678");
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        result.PhoneNumber.Should().Be("0912345678");
    }

    /// <summary>
    /// Tài khoản không có số điện thoại phải trả về PhoneNumber = null,
    /// không được trả về chuỗi rỗng vì client dùng null-check để ẩn trường này.
    /// </summary>
    [Test]
    public async Task HandleAsync_UserWithNoPhone_ReturnsNullPhoneNumber()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("patient3", "patient3@test.com", "hash", "Patient");
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        result.PhoneNumber.Should().BeNull();
    }

    /// <summary>
    /// Tài khoản mới chưa điền hồ sơ phải trả về các trường optional đều null,
    /// tránh hiển thị giá trị rác trên màn hình hồ sơ bệnh nhân.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewUser_ReturnsNullOptionalFields()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("patient4", "patient4@test.com", "hash", "Patient");
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        result.DateOfBirth.Should().BeNull();
        result.Gender.Should().BeNull();
        result.ProfilePictureUrl.Should().BeNull();
    }

    // ── Role-based salary computation ────────────────────────────────────────

    /// <summary>
    /// Bác sĩ (Dentist) phải nhận lương cơ bản 40 triệu và phụ cấp tính theo
    /// số năm kinh nghiệm (2 triệu/năm), đúng theo quy tắc tính lương của phòng khám.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistRole_ReturnsCorrectBaseSalaryAndAllowance()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("dentist1", "dentist1@test.com", "hash", "Dentist");
        user.SetStaffProfile(new StaffProfileData(
            EmployeeId: null, Department: null, EmploymentStatus: null, ProfilePictureUrl: null,
            ProfessionalNotes: null, Specialty: null, LicenseNumber: null, YearsOfExperience: 5,
            Gender: null, DateOfBirth: null, Address: null, StartDate: null, ServicesHandled: null,
            CertificateIssuedDate: null, CertificateIssuedBy: null, Education: null, Bio: null,
            Position: null, EmploymentType: null, BaseSalary: null, SalaryUnit: null,
            LeaveAccrued: null, Allowance: null));
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        result.BaseSalary.Should().Be(40000000);
        result.Allowance.Should().Be(10000000); // 5 năm * 2,000,000
        result.SalaryNote.Should().Contain("bác sĩ");
    }

    /// <summary>
    /// Bác sĩ chưa khai số năm kinh nghiệm (YearsOfExperience = null) phải nhận
    /// phụ cấp bằng 0, không được để phép tính gây lỗi NullReferenceException.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistWithNoYearsOfExperience_AllowanceIsZero()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("dentist2", "dentist2@test.com", "hash", "Dentist");
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        result.Allowance.Should().Be(0);
    }

    /// <summary>
    /// Quản trị viên (Admin) phải nhận lương cơ bản 25 triệu và phụ cấp cố định 5 triệu,
    /// khác với công thức tính theo kinh nghiệm của bác sĩ.
    /// </summary>
    [Test]
    public async Task HandleAsync_AdminRole_ReturnsCorrectBaseSalaryAndAllowance()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("admin1", "admin1@test.com", "hash", "Admin");
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        result.BaseSalary.Should().Be(25000000);
        result.Allowance.Should().Be(5000000);
        result.SalaryNote.Should().Contain("quản lý");
    }

    /// <summary>
    /// Nhân viên hành chính (Staff) phải nhận lương cơ bản 12 triệu và phụ cấp 2 triệu,
    /// đúng theo mức lương riêng dành cho nhóm vai trò này.
    /// </summary>
    [Test]
    public async Task HandleAsync_StaffRole_ReturnsCorrectBaseSalaryAndAllowance()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("staff1", "staff1@test.com", "hash", "Staff");
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        result.BaseSalary.Should().Be(12000000);
        result.Allowance.Should().Be(2000000);
        result.SalaryNote.Should().Contain("hành chính");
    }

    /// <summary>
    /// Bệnh nhân (Patient) không thuộc nhóm role có lương, phải trả về BaseSalary = 0,
    /// Allowance = 0 và SalaryNote rỗng — không rơi vào bất kỳ nhánh tính lương nào.
    /// </summary>
    [Test]
    public async Task HandleAsync_PatientRole_ReturnsZeroSalaryAndEmptyNote()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("patient5", "patient5@test.com", "hash", "Patient");
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        result.BaseSalary.Should().Be(0);
        result.Allowance.Should().Be(0);
        result.SalaryNote.Should().BeEmpty();
    }

    /// <summary>
    /// Tài khoản chưa có FullName (null) phải trả về FullName = chuỗi rỗng thay vì null,
    /// tránh lỗi hiển thị hoặc NullReferenceException phía client khi bind dữ liệu.
    /// </summary>
    [Test]
    public async Task HandleAsync_UserWithNoFullName_ReturnsEmptyStringFullName()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("patient6", "patient6@test.com", "hash", "Patient");
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        result.FullName.Should().BeEmpty();
    }

    // ── Error path ────────────────────────────────────────────────────────────

    /// <summary>
    /// userId không tồn tại trong hệ thống phải ném NotFoundException,
    /// để controller trả về 404 thay vì 500 khi token hợp lệ nhưng user đã bị xóa.
    /// </summary>
    [Test]
    public async Task HandleAsync_UserNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Khi userId không tồn tại, handler không được cố tiếp tục xử lý
    /// mà phải dừng ngay tại bước tìm kiếm để tránh NullReferenceException downstream.
    /// </summary>
    [Test]
    public async Task HandleAsync_UserNotFound_DoesNotReturnResult()
    {
        var userId = Guid.NewGuid();
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = async () => await _handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        // Assertion: kiểm tra ném exception, không trả về dto
    }
}
