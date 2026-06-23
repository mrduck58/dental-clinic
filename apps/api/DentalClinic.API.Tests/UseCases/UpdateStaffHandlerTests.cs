using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Moq;

namespace DentalClinic.API.Tests.UseCases;

[TestFixture]
public class UpdateStaffHandlerTests
{
    private Mock<IUserRepository> _mockRepo = null!;
    private UpdateStaffHandler _handler = null!;
    private static readonly Guid ExistingId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IUserRepository>();
        _handler = new UpdateStaffHandler(_mockRepo.Object);
    }

    private static User CreateExistingUser()
    {
        var user = User.CreateEmployee("old@example.com", "Staff", "0901234567", "Nguyễn Cũ");
        // Use reflection to set the Id since it's private set
        typeof(User).GetProperty("Id")!.SetValue(user, ExistingId);
        return user;
    }

    private static UpdateStaffCommand BuildValidCommand(
        Guid? id = null,
        string fullName = "Nguyễn Văn B",
        string email = "old@example.com",
        string phoneNumber = "0901234567",
        string role = "Staff",
        string? employmentType = "Full-time",
        decimal? baseSalary = 15000000m,
        string? salaryUnit = "Theo tháng",
        decimal? leaveAccrued = 1.5m)
    {
        return new UpdateStaffCommand(
            Id: id ?? ExistingId,
            FullName: fullName,
            Email: email,
            PhoneNumber: phoneNumber,
            Role: role,
            Department: "Hành chính",
            EmploymentStatus: "Active",
            ProfilePictureUrl: null,
            ProfessionalNotes: null,
            IsActive: true,
            Specialty: null,
            LicenseNumber: null,
            YearsOfExperience: null,
            Gender: "Nam",
            DateOfBirth: new DateOnly(1995, 5, 15),
            Address: "456 Đường XYZ",
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

    // ── Normal: Valid update ─────────────────────────────────────────────────

    [Test]
    public async Task HandleAsync_ValidUpdate_SameEmail_ReturnsDto()
    {
        var existing = CreateExistingUser();
        _mockRepo.Setup(r => r.GetByIdAsync(ExistingId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var command = BuildValidCommand();
        var result = await _handler.HandleAsync(command);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.FullName, Is.EqualTo("Nguyễn Văn B"));
        Assert.That(result.EmploymentType, Is.EqualTo("Full-time"));
        Assert.That(result.BaseSalary, Is.EqualTo(15000000m));
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_ValidUpdate_ChangedEmail_NotDuplicate_ReturnsDto()
    {
        var existing = CreateExistingUser();
        _mockRepo.Setup(r => r.GetByIdAsync(ExistingId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);
        _mockRepo.Setup(r => r.ExistsByEmailAsync("new@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var command = BuildValidCommand(email: "new@example.com");
        var result = await _handler.HandleAsync(command);

        Assert.That(result.Email, Is.EqualTo("new@example.com"));
    }

    // ── Abnormal: Not found ─────────────────────────────────────────────────

    [Test]
    public void HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(ExistingId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var command = BuildValidCommand();
        Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
    }

    // ── Abnormal: Email conflict ────────────────────────────────────────────

    [Test]
    public void HandleAsync_EmailConflict_ThrowsConflict()
    {
        var existing = CreateExistingUser();
        _mockRepo.Setup(r => r.GetByIdAsync(ExistingId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);
        _mockRepo.Setup(r => r.ExistsByEmailAsync("taken@example.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var command = BuildValidCommand(email: "taken@example.com");
        Assert.ThrowsAsync<ConflictException>(() => _handler.HandleAsync(command));
    }

    // ── Abnormal: Validation errors ─────────────────────────────────────────

    [Test]
    public void HandleAsync_EmptyFullName_ThrowsValidation()
    {
        var command = BuildValidCommand(fullName: "");
        var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.That(ex!.Errors.ContainsKey("fullName"), Is.True);
        // Repo should NOT be called at all
        _mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_InvalidRole_ThrowsValidation()
    {
        var command = BuildValidCommand(role: "Hacker");
        var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.That(ex!.Errors.ContainsKey("role"), Is.True);
    }

    [Test]
    public void HandleAsync_NegativeLeaveAccrued_ThrowsValidation()
    {
        var command = BuildValidCommand(leaveAccrued: -2m);
        var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.That(ex!.Errors.ContainsKey("leaveAccrued"), Is.True);
    }

    [Test]
    public void HandleAsync_InvalidEmploymentType_ThrowsValidation()
    {
        var command = BuildValidCommand(employmentType: "Contract");
        var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.That(ex!.Errors.ContainsKey("employmentType"), Is.True);
    }

    // ── Boundary: Salary edge cases ─────────────────────────────────────────

    [Test]
    public async Task HandleAsync_SalaryZero_Succeeds()
    {
        var existing = CreateExistingUser();
        _mockRepo.Setup(r => r.GetByIdAsync(ExistingId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var command = BuildValidCommand(baseSalary: 0m);
        var result = await _handler.HandleAsync(command);
        Assert.That(result.BaseSalary, Is.EqualTo(0m));
    }

    [Test]
    public void HandleAsync_SalaryExceedsMax_ThrowsValidation()
    {
        var command = BuildValidCommand(baseSalary: 1_000_000_000m);
        Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
    }
}
