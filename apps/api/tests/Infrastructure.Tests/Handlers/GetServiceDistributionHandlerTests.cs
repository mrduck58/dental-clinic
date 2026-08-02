using DentalClinic.API.Application.UseCases.Dashboard;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetServiceDistributionHandlerTests
{
    private AppDbContext _db = null!;
    private GetServiceDistributionHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetServiceDistributionHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(Patient patient, Dentist dentist)> SeedBasicDataAsync(
        string dentistName = "BS. Nguyễn Văn A", string specialization = "Nha khoa tổng quát")
    {
        var patientUser = User.Create("p1", $"p1-{Guid.NewGuid()}@test.com", "hash", "Patient", fullName: "Trần Thị B");
        var dentistUser = User.Create("d1", $"d1-{Guid.NewGuid()}@test.com", "hash", "Dentist", fullName: dentistName);
        _db.Users.AddRange(patientUser, dentistUser);

        var dentist = Dentist.Create(dentistUser.Id, specialization, 5);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nữ");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);

        await _db.SaveChangesAsync();
        return (patient, dentist);
    }

    /// <summary>Phân bổ dịch vụ phải nhóm đúng theo Service và tính % chính xác trên tổng.</summary>
    [Test]
    public async Task Handle_GroupsByServiceAndComputesPercentage()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var service = Service.Create("Cấy ghép Implant", 10_000_000m, 60, "Mô tả");
        _db.Services.Add(service);
        _db.Appointments.AddRange(
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id),
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id),
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow)); // không có dịch vụ
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetServiceDistributionQuery("week", 5), CancellationToken.None);

        result.TotalAppointments.Should().Be(3);
        var serviceItem = result.Items.Single(i => i.ServiceId == service.Id);
        serviceItem.ServiceName.Should().Be("Cấy ghép Implant");
        serviceItem.Count.Should().Be(2);
        serviceItem.Percentage.Should().BeApproximately(66.7, 0.1);
    }

    /// <summary>Khi số dịch vụ vượt topN, phần còn lại phải gộp vào 1 mục "khác" (ServiceId null).</summary>
    [Test]
    public async Task Handle_ExceedsTopN_AggregatesRestIntoOtherBucket()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var services = Enumerable.Range(1, 3)
            .Select(i => Service.Create($"Dịch vụ {i}", 100_000m, 30, "Mô tả"))
            .ToList();
        _db.Services.AddRange(services);
        foreach (var service in services)
            _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetServiceDistributionQuery("week", 1), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Should().ContainSingle(i => i.ServiceId == null && i.Count == 2);
    }

    [Test]
    public async Task Handle_NoAppointments_ReturnsEmptyItems()
    {
        var result = await _handler.Handle(new GetServiceDistributionQuery("week", 5), CancellationToken.None);

        result.TotalAppointments.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    /// <summary>topN &lt;= 0 phải được kẹp về tối thiểu 1 (chỉ dịch vụ đông nhất + 1 mục "khác"),
    /// không được ném lỗi hay trả về 0 mục.</summary>
    [Test]
    public async Task Handle_TopNBelowMinimum_ClampsToOne()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var services = Enumerable.Range(1, 3)
            .Select(i => Service.Create($"Dịch vụ {i}", 100_000m, 30, "Mô tả"))
            .ToList();
        _db.Services.AddRange(services);
        foreach (var service in services)
            _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetServiceDistributionQuery("week", 0), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Count(i => i.ServiceId != null).Should().Be(1);
    }

    /// <summary>topN vượt quá 20 phải được kẹp về tối đa 20.</summary>
    [Test]
    public async Task Handle_TopNAboveMaximum_ClampsToTwenty()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var services = Enumerable.Range(1, 3)
            .Select(i => Service.Create($"Dịch vụ {i}", 100_000m, 30, "Mô tả"))
            .ToList();
        _db.Services.AddRange(services);
        foreach (var service in services)
            _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetServiceDistributionQuery("week", 100), CancellationToken.None);

        result.Items.Should().HaveCount(3);
        result.Items.Should().OnlyContain(i => i.ServiceId != null);
    }
}
