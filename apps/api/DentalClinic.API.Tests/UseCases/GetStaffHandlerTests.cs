using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Moq;

namespace DentalClinic.API.Tests.UseCases;

[TestFixture]
public class GetStaffHandlerTests
{
    private Mock<IUserRepository> _mockRepo = null!;
    private GetStaffHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IUserRepository>();
        _handler = new GetStaffHandler(_mockRepo.Object);
    }

    private static User CreateSampleUser(string email, string role, string fullName)
    {
        return User.CreateEmployee(email, role, "0901234567", fullName);
    }

    // ── Normal: Pagination ──────────────────────────────────────────────────

    [Test]
    public async Task HandleAsync_ReturnsPaginatedResult()
    {
        var users = new List<User>
        {
            CreateSampleUser("user1@test.com", "Staff", "User 1"),
            CreateSampleUser("user2@test.com", "Dentist", "User 2"),
        };

        _mockRepo.Setup(r => r.GetStaffPagedAsync(null, null, null, 1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((users.AsReadOnly(), 2));
        _mockRepo.Setup(r => r.GetStaffStatsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new StaffStatsResult(2, 1, 0));

        var result = await _handler.HandleAsync(new GetStaffQuery(null, null, null, 1, 10));

        Assert.That(result.Items.Count, Is.EqualTo(2));
        Assert.That(result.TotalCount, Is.EqualTo(2));
        Assert.That(result.Page, Is.EqualTo(1));
        Assert.That(result.PageSize, Is.EqualTo(10));
        Assert.That(result.Statistics.TotalEmployees, Is.EqualTo(2));
    }

    // ── Boundary: Page clamping ─────────────────────────────────────────────

    [Test]
    public async Task HandleAsync_PageZero_ClampedToOne()
    {
        _mockRepo.Setup(r => r.GetStaffPagedAsync(null, null, null, 1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((new List<User>().AsReadOnly(), 0));
        _mockRepo.Setup(r => r.GetStaffStatsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new StaffStatsResult(0, 0, 0));

        var result = await _handler.HandleAsync(new GetStaffQuery(null, null, null, 0, 10));

        Assert.That(result.Page, Is.EqualTo(1));
    }

    [Test]
    public async Task HandleAsync_NegativePage_ClampedToOne()
    {
        _mockRepo.Setup(r => r.GetStaffPagedAsync(null, null, null, 1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((new List<User>().AsReadOnly(), 0));
        _mockRepo.Setup(r => r.GetStaffStatsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new StaffStatsResult(0, 0, 0));

        var result = await _handler.HandleAsync(new GetStaffQuery(null, null, null, -5, 10));

        Assert.That(result.Page, Is.EqualTo(1));
    }

    [Test]
    public async Task HandleAsync_PageSizeZero_ClampedToOne()
    {
        _mockRepo.Setup(r => r.GetStaffPagedAsync(null, null, null, 1, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((new List<User>().AsReadOnly(), 0));
        _mockRepo.Setup(r => r.GetStaffStatsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new StaffStatsResult(0, 0, 0));

        var result = await _handler.HandleAsync(new GetStaffQuery(null, null, null, 1, 0));

        Assert.That(result.PageSize, Is.EqualTo(1));
    }

    [Test]
    public async Task HandleAsync_PageSizeOverMax_ClampedTo100()
    {
        _mockRepo.Setup(r => r.GetStaffPagedAsync(null, null, null, 1, 100, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((new List<User>().AsReadOnly(), 0));
        _mockRepo.Setup(r => r.GetStaffStatsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new StaffStatsResult(0, 0, 0));

        var result = await _handler.HandleAsync(new GetStaffQuery(null, null, null, 1, 500));

        Assert.That(result.PageSize, Is.EqualTo(100));
    }

    // ── Normal: Filter by role ──────────────────────────────────────────────

    [Test]
    public async Task HandleAsync_FilterByRole_PassesRoleToRepo()
    {
        _mockRepo.Setup(r => r.GetStaffPagedAsync(null, "Dentist", null, 1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((new List<User>().AsReadOnly(), 0));
        _mockRepo.Setup(r => r.GetStaffStatsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new StaffStatsResult(0, 0, 0));

        await _handler.HandleAsync(new GetStaffQuery(null, "Dentist", null, 1, 10));

        _mockRepo.Verify(r => r.GetStaffPagedAsync(null, "Dentist", null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Normal: Search ──────────────────────────────────────────────────────

    [Test]
    public async Task HandleAsync_SearchTerm_PassesToRepo()
    {
        _mockRepo.Setup(r => r.GetStaffPagedAsync("nguyễn", null, null, 1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((new List<User>().AsReadOnly(), 0));
        _mockRepo.Setup(r => r.GetStaffStatsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new StaffStatsResult(0, 0, 0));

        await _handler.HandleAsync(new GetStaffQuery("nguyễn", null, null, 1, 10));

        _mockRepo.Verify(r => r.GetStaffPagedAsync("nguyễn", null, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Normal: GetById ─────────────────────────────────────────────────────

    [Test]
    public async Task GetByIdAsync_Found_ReturnsDto()
    {
        var user = CreateSampleUser("found@test.com", "Staff", "Found User");
        _mockRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var result = await _handler.GetByIdAsync(user.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Email, Is.EqualTo("found@test.com"));
        Assert.That(result.FullName, Is.EqualTo("Found User"));
    }

    // ── Abnormal: GetById not found ─────────────────────────────────────────

    [Test]
    public void GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _handler.GetByIdAsync(id));
    }

    // ── Normal: ToDto maps new salary fields ────────────────────────────────

    [Test]
    public void ToDto_MapsSalaryFields()
    {
        var user = CreateSampleUser("dto@test.com", "Staff", "DTO User");
        user.SetStaffProfile(new StaffProfileData(
            EmployeeId: "NV-01",
            Department: "IT",
            EmploymentStatus: "Active",
            ProfilePictureUrl: null,
            ProfessionalNotes: null,
            Specialty: null,
            LicenseNumber: null,
            YearsOfExperience: null,
            Gender: "Nam",
            DateOfBirth: new DateOnly(1995, 1, 1),
            Address: null,
            StartDate: null,
            ServicesHandled: null,
            CertificateIssuedDate: null,
            CertificateIssuedBy: null,
            Education: null,
            Bio: null,
            Position: null,
            EmploymentType: "Part-time",
            BaseSalary: 8_000_000m,
            SalaryUnit: "Theo ngày",
            LeaveAccrued: 0.5m));

        var dto = GetStaffHandler.ToDto(user);

        Assert.That(dto.EmploymentType, Is.EqualTo("Part-time"));
        Assert.That(dto.BaseSalary, Is.EqualTo(8_000_000m));
        Assert.That(dto.SalaryUnit, Is.EqualTo("Theo ngày"));
        Assert.That(dto.LeaveAccrued, Is.EqualTo(0.5m));
    }

    // ── Normal: Empty results ───────────────────────────────────────────────

    [Test]
    public async Task HandleAsync_NoResults_ReturnsEmptyList()
    {
        _mockRepo.Setup(r => r.GetStaffPagedAsync(null, null, null, 1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((new List<User>().AsReadOnly(), 0));
        _mockRepo.Setup(r => r.GetStaffStatsAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new StaffStatsResult(0, 0, 0));

        var result = await _handler.HandleAsync(new GetStaffQuery(null, null, null, 1, 10));

        Assert.That(result.Items, Is.Empty);
        Assert.That(result.TotalCount, Is.EqualTo(0));
    }
}
