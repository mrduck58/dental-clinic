using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Staff;

[TestFixture]
public class UpdateStaffHandlerTests
{
    private IUserRepository _userRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    /// <summary>
    /// Cập nhật thông tin nhân viên tồn tại phải gọi UpdateAsync 1 lần.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingStaff_CallsUpdateAsyncOnce()
    {
        var user = MakeEmployee();
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new UpdateStaffHandler(_userRepo);

        await handler.HandleAsync(BuildCommand(user.Id, email: user.Email));

        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Nhân viên không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new UpdateStaffHandler(_userRepo);

        Func<Task> act = () => handler.HandleAsync(BuildCommand(Guid.NewGuid()));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Đổi sang email mới đã tồn tại trong hệ thống phải ném ConflictException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmailAlreadyTaken_ThrowsConflictException()
    {
        var user = MakeEmployee(email: "old@test.com");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _userRepo.ExistsByEmailAsync("new@test.com", Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateStaffHandler(_userRepo);

        Func<Task> act = () => handler.HandleAsync(BuildCommand(user.Id, email: "new@test.com"));

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>
    /// Giữ nguyên email cũ khi update không được kiểm tra conflict,
    /// tránh lỗi khi email của chính nhân viên đó bị coi là "đã tồn tại".
    /// </summary>
    [Test]
    public async Task HandleAsync_SameEmail_DoesNotCheckEmailConflict()
    {
        var user = MakeEmployee(email: "same@test.com");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new UpdateStaffHandler(_userRepo);

        await handler.HandleAsync(BuildCommand(user.Id, email: "same@test.com"));

        await _userRepo.DidNotReceive().ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static User MakeEmployee(string email = "emp@test.com")
        => User.CreateEmployee(email, "Staff", "0901234567", "Nhân Viên Test");

    private static UpdateStaffCommand BuildCommand(Guid id, string email = "emp@test.com")
        => new(Id: id, FullName: "Tên Mới", Email: email, PhoneNumber: "0901234567",
            Role: "Staff", Department: null, EmploymentStatus: null,
            ProfilePictureUrl: null, ProfessionalNotes: null, IsActive: true,
            Specialty: null, LicenseNumber: null, YearsOfExperience: null,
            Gender: null, DateOfBirth: null, Address: null, StartDate: null,
            ServicesHandled: null, CertificateIssuedDate: null, CertificateIssuedBy: null,
            Education: null, Bio: null, Position: null,
            EmploymentType: null, BaseSalary: null, SalaryUnit: null, LeaveAccrued: null);
}
