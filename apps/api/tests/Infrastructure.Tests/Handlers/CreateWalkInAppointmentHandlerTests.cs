using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class CreateWalkInAppointmentHandlerTests
{
    private AppDbContext _db = null!;
    private CreateWalkInAppointmentHandler _handler = null!;
    private Guid _dentistId;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new CreateWalkInAppointmentHandler(_db);

        var dentistUser = User.Create("d1", $"d1-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "BS. Nguyễn Văn Hùng", "Nha khoa tổng quát", 5);
        _db.Dentists.Add(dentist);
        await _db.SaveChangesAsync();
        _dentistId = dentist.Id;
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private CreateWalkInCommand MakeCommand(DateTimeOffset appointmentDate) => new(
        _dentistId,
        appointmentDate,
        "Nguyễn Văn A",
        "0901234567",
        new DateOnly(1990, 1, 1),
        "Nam",
        null,
        null);

    /// <summary>Không cho đặt lịch cho khung giờ đã qua — chặn cả trường hợp bypass UI.</summary>
    [Test]
    public async Task HandleAsync_PastAppointmentDate_ThrowsValidationException()
    {
        var pastDate = DateTimeOffset.UtcNow.AddHours(-1);

        Func<Task> act = async () => await _handler.HandleAsync(MakeCommand(pastDate));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task HandleAsync_FutureAppointmentDate_CreatesConfirmedAppointment()
    {
        var futureDate = DateTimeOffset.UtcNow.AddHours(1);

        var result = await _handler.HandleAsync(MakeCommand(futureDate));

        result.Status.Should().Be("Confirmed");
        result.PatientName.Should().Be("Nguyễn Văn A");
    }

    [Test]
    public async Task HandleAsync_SlotAlreadyBooked_ThrowsConflictException()
    {
        var futureDate = DateTimeOffset.UtcNow.AddHours(1);
        await _handler.HandleAsync(MakeCommand(futureDate));

        Func<Task> act = async () => await _handler.HandleAsync(MakeCommand(futureDate));

        await act.Should().ThrowAsync<ConflictException>();
    }
}
