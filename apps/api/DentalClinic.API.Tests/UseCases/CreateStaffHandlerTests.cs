using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Moq;

namespace DentalClinic.API.Tests.UseCases;

[TestFixture]
public class CreateStaffHandlerTests
{
    private Mock<IUserRepository> _mockRepo = null!;
    private CreateStaffHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IUserRepository>();
        _handler = new CreateStaffHandler(_mockRepo.Object);
    }

    private static CreateStaffCommand BuildValidCommand(
        string fullName = "Nguyễn Văn A",
        string email = "newstaff@example.com",
        string phoneNumber = "0901234567",
        string role = "Staff",
        string? specialty = null,
        string? licenseNumber = null,
        int? yearsOfExperience = null,
        string? employmentType = "Full-time",
        decimal? baseSalary = 12000000m,
        string? salaryUnit = "Theo tháng",
        decimal? leaveAccrued = 1.0m)
    {
        return new CreateStaffCommand(
            FullName: fullName,
            Email: email,
            PhoneNumber: phoneNumber,
            Role: role,
            EmployeeId: "NV-01",
            Department: "Hành chính",
            EmploymentStatus: "Active",
            ProfilePictureUrl: null,
            ProfessionalNotes: null,
            Specialty: specialty,
            LicenseNumber: licenseNumber,
            YearsOfExperience: yearsOfExperience,
            Gender: "Nam",
            DateOfBirth: new DateOnly(1995, 5, 15),
            Address: "123 Đường ABC",
            StartDate: new DateOnly(2024, 1, 15),
            ServicesHandled: null,
            CertificateIssuedDate: null,
            CertificateIssuedBy: null,
            Education: "Đại học",
            Bio: null,
            Position: "Lễ tân",
            EmploymentType: employmentType,
            BaseSalary: baseSalary,
            SalaryUnit: salaryUnit,
            LeaveAccrued: leaveAccrued);
    }

    // ── Normal: Valid create ─────────────────────────────────────────────────

    [Test]
    public async Task HandleAsync_ValidStaffData_ReturnsDto()
    {
        _mockRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var command = BuildValidCommand();
        var result = await _handler.HandleAsync(command);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Email, Is.EqualTo("newstaff@example.com"));
        Assert.That(result.FullName, Is.EqualTo("Nguyễn Văn A"));
        Assert.That(result.Role, Is.EqualTo("Staff"));
        Assert.That(result.EmploymentType, Is.EqualTo("Full-time"));
        Assert.That(result.BaseSalary, Is.EqualTo(12000000m));
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_ValidDoctorData_ReturnsDto()
    {
        _mockRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var command = BuildValidCommand(
            role: "Dentist",
            specialty: "Nha khoa tổng quát",
            licenseNumber: "BS-12345",
            yearsOfExperience: 10);

        var result = await _handler.HandleAsync(command);

        Assert.That(result.Role, Is.EqualTo("Dentist"));
        Assert.That(result.Specialty, Is.EqualTo("Nha khoa tổng quát"));
        Assert.That(result.LicenseNumber, Is.EqualTo("BS-12345"));
    }

    // ── Abnormal: Duplicate email ───────────────────────────────────────────

    [Test]
    public void HandleAsync_DuplicateEmail_ThrowsConflict()
    {
        _mockRepo.Setup(r => r.ExistsByEmailAsync("existing@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var command = BuildValidCommand(email: "existing@example.com");

        Assert.ThrowsAsync<ConflictException>(() => _handler.HandleAsync(command));
    }

    // ── Abnormal: Validation fails before repo call ─────────────────────────

    [Test]
    public void HandleAsync_EmptyFullName_ThrowsValidation()
    {
        var command = BuildValidCommand(fullName: "");
        var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.That(ex!.Errors.ContainsKey("fullName"), Is.True);
        // Repo should NOT be called
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_InvalidEmail_ThrowsValidation()
    {
        var command = BuildValidCommand(email: "not-an-email");
        var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.That(ex!.Errors.ContainsKey("email"), Is.True);
    }

    [Test]
    public void HandleAsync_InvalidRole_ThrowsValidation()
    {
        var command = BuildValidCommand(role: "Superman");
        var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.That(ex!.Errors.ContainsKey("role"), Is.True);
    }

    [Test]
    public void HandleAsync_NegativeSalary_ThrowsValidation()
    {
        var command = BuildValidCommand(baseSalary: -5000m);
        var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.That(ex!.Errors.ContainsKey("baseSalary"), Is.True);
    }

    [Test]
    public void HandleAsync_FullTimeWithTheoCa_ThrowsValidation()
    {
        var command = BuildValidCommand(employmentType: "Full-time", salaryUnit: "Theo ca");
        var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.That(ex!.Errors.ContainsKey("salaryUnit"), Is.True);
    }

    [Test]
    public void HandleAsync_DentistMissingSpecialty_ThrowsValidation()
    {
        var command = BuildValidCommand(role: "Dentist", specialty: null, licenseNumber: "BS-001");
        var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.That(ex!.Errors.ContainsKey("specialty"), Is.True);
    }

    [Test]
    public void HandleAsync_DentistMissingLicense_ThrowsValidation()
    {
        var command = BuildValidCommand(role: "Dentist", specialty: "Nha khoa", licenseNumber: null);
        var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.That(ex!.Errors.ContainsKey("licenseNumber"), Is.True);
    }

    // ── Boundary: Salary at limits ──────────────────────────────────────────

    [Test]
    public async Task HandleAsync_SalaryAtMaxBoundary_Succeeds()
    {
        _mockRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var command = BuildValidCommand(baseSalary: 999_999_999m);
        var result = await _handler.HandleAsync(command);
        Assert.That(result.BaseSalary, Is.EqualTo(999_999_999m));
    }

    [Test]
    public void HandleAsync_SalaryOverMax_ThrowsValidation()
    {
        var command = BuildValidCommand(baseSalary: 1_000_000_000m);
        Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
    }
}
