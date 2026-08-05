using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Staff;

[TestFixture]
public class CreateStaffHandlerTests
{
    private IUserRepository _userRepo = null!;
    private IEmployeeRepository _employeeRepo = null!;
    private IDentistRepository _dentistRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepo = Substitute.For<IEmployeeRepository>();
        _dentistRepo = Substitute.For<IDentistRepository>();
    }

    /// <summary>
    /// Tạo hồ sơ nhân viên mới với email chưa tồn tại phải gọi AddAsync 1 lần.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_CallsAddAsyncOnce()
    {
        var handler = new CreateStaffHandler(_userRepo, _employeeRepo, _dentistRepo);

        await handler.Handle(BuildCommand(), CancellationToken.None);

        await _userRepo.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Nhân viên mới tạo qua CreateStaffHandler không được có tài khoản đăng nhập,
    /// tài khoản chỉ được cấp riêng qua CreateStaffAccountHandler sau này.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_ReturnedDtoHasNoAccount()
    {
        var handler = new CreateStaffHandler(_userRepo, _employeeRepo, _dentistRepo);

        var result = await handler.Handle(BuildCommand(), CancellationToken.None);

        result.HasAccount.Should().BeFalse();
    }

    /// <summary>
    /// Email đã được dùng bởi nhân viên khác phải ném ConflictException.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateEmail_ThrowsConflictException()
    {
        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateStaffHandler(_userRepo, _employeeRepo, _dentistRepo);

        Func<Task> act = () => handler.Handle(BuildCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>
    /// Khi email trùng, AddAsync không được gọi để tránh lưu dữ liệu trùng lặp.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateEmail_DoesNotCallAddAsync()
    {
        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateStaffHandler(_userRepo, _employeeRepo, _dentistRepo);

        Assert.CatchAsync(() => handler.Handle(BuildCommand(), CancellationToken.None));

        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dữ liệu không hợp lệ (họ tên rỗng) phải ném ValidationException ngay từ bước validate,
    /// trước khi kiểm tra trùng email hay gọi AddAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_InvalidFullName_ThrowsValidationExceptionBeforeAnyRepoCall()
    {
        var handler = new CreateStaffHandler(_userRepo, _employeeRepo, _dentistRepo);

        Func<Task> act = () => handler.Handle(BuildCommand() with { FullName = "" }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _userRepo.DidNotReceive().ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Vai trò "Dentist" bắt buộc phải có Specialty và LicenseNumber; thiếu các trường này
    /// phải ném ValidationException dù các trường khác hợp lệ.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistRoleWithoutSpecialtyOrLicense_ThrowsValidationException()
    {
        var handler = new CreateStaffHandler(_userRepo, _employeeRepo, _dentistRepo);

        Func<Task> act = () => handler.Handle(BuildCommand() with { Role = "Dentist", Specialty = null, LicenseNumber = null }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Số điện thoại sai định dạng (chứa chữ cái) phải ném ValidationException.
    /// </summary>
    [Test]
    public async Task HandleAsync_InvalidPhoneNumber_ThrowsValidationException()
    {
        var handler = new CreateStaffHandler(_userRepo, _employeeRepo, _dentistRepo);

        Func<Task> act = () => handler.Handle(BuildCommand() with { PhoneNumber = "abc-not-a-phone" }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private static CreateStaffCommand BuildCommand(string email = "newstaff@test.com")
        => new(FullName: "Nhân Viên Mới", Email: email, PhoneNumber: "0901234567",
            Role: "Staff", EmployeeId: null, Department: null, EmploymentStatus: null,
            ProfilePictureUrl: null, ProfessionalNotes: null, Specialty: null,
            LicenseNumber: null, YearsOfExperience: null, Gender: null, DateOfBirth: null,
            Address: null, StartDate: null, ServicesHandled: null,
            CertificateIssuedDate: null, CertificateIssuedBy: null,
            Education: null, Bio: null, Position: null,
            EmploymentType: null, BaseSalary: null, SalaryUnit: null, LeaveAccrued: null, Allowance: null);
}
