using DentalClinic.API.Application.UseCases.Dentists;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class DentistReviewHandlerTests
{
    private AppDbContext _db = null!;
    private DentistReviewRepository _reviewRepository = null!;
    private AppointmentRepository _appointmentRepository = null!;
    private PatientRepository _patientRepository = null!;
    private GetDentistReviewsHandler _getHandler = null!;
    private UpsertDentistReviewHandler _upsertHandler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _reviewRepository = new DentistReviewRepository(_db);
        _appointmentRepository = new AppointmentRepository(_db);
        _patientRepository = new PatientRepository(_db);

        _getHandler = new GetDentistReviewsHandler(_reviewRepository);
        _upsertHandler = new UpsertDentistReviewHandler(_reviewRepository, _appointmentRepository, _patientRepository);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Tạo dentist + user liên kết, trả về (dentist, user) đã lưu DB.</summary>
    private async Task<Dentist> SeedDentistAsync(string username)
    {
        var dentistUser = User.Create(username, $"{username}-{Guid.NewGuid()}@test.com", "hash", "Dentist", fullName: "BS. Test");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        _db.Dentists.Add(dentist);
        await _db.SaveChangesAsync();
        return dentist;
    }

    /// <summary>Tạo patient + user liên kết, trả về (patient, userId) đã lưu DB.</summary>
    private async Task<Patient> SeedPatientAsync(string username, string fullName = "Bệnh nhân Test")
    {
        var patientUser = User.Create(username, $"{username}-{Guid.NewGuid()}@test.com", "hash", "Patient", fullName: fullName);
        _db.Users.Add(patientUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return patient;
    }

    /// <summary>Tạo 1 buổi khám Completed giữa patient và dentist — điều kiện để được phép đánh giá.</summary>
    private async Task SeedCompletedVisitAsync(Guid dentistId, Guid patientId)
    {
        var appointment = Appointment.Create(patientId, dentistId, DateTimeOffset.UtcNow.AddDays(-1));
        appointment.Complete();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
    }

    // ---------- GetDentistReviewsHandler ----------

    /// <summary>Nha sĩ chưa có đánh giá nào phải trả về điểm trung bình 0, số lượng 0, danh sách rỗng.</summary>
    [Test]
    public async Task HandleAsync_NoReviews_ReturnsZeroAverageAndEmptyList()
    {
        var dentist = await SeedDentistAsync("gr1");

        var result = await _getHandler.Handle(new GetDentistReviewsQuery(dentist.Id), CancellationToken.None);

        result.AverageRating.Should().Be(0);
        result.ReviewCount.Should().Be(0);
        result.Reviews.Should().BeEmpty();
    }

    /// <summary>Điểm trung bình phải được tính đúng và làm tròn 1 chữ số thập phân từ nhiều đánh giá.</summary>
    [Test]
    public async Task HandleAsync_MultipleReviews_CalculatesAverageRatingRoundedToOneDecimal()
    {
        var dentist = await SeedDentistAsync("gr2");
        var patientA = await SeedPatientAsync("gr2-pa");
        var patientB = await SeedPatientAsync("gr2-pb");
        var patientC = await SeedPatientAsync("gr2-pc");

        _db.DentistReviews.AddRange(
            DentistReview.Create(dentist.Id, patientA.Id, 5, "Tuyệt vời"),
            DentistReview.Create(dentist.Id, patientB.Id, 5, "Rất tốt"),
            DentistReview.Create(dentist.Id, patientC.Id, 3, "Bình thường"));
        await _db.SaveChangesAsync();

        var result = await _getHandler.Handle(new GetDentistReviewsQuery(dentist.Id), CancellationToken.None);

        result.ReviewCount.Should().Be(3);
        result.AverageRating.Should().Be(4.3); // (5+5+3)/3 = 4.333... -> 4.3
    }

    /// <summary>DTO trả về phải map đúng PatientName (kèm User), Rating, Comment và tách Tags từ chuỗi CSV
    /// lưu trong DB. (Trước đây DentistReviewRepository.GetByDentistIdAsync thiếu ThenInclude(p => p.User) khiến
    /// PatientName luôn rỗng — đã vá bằng cách thêm ThenInclude vào query AsNoTracking.)</summary>
    [Test]
    public async Task HandleAsync_ReviewWithTags_MapsAllFieldsCorrectlyIntoDto()
    {
        var dentist = await SeedDentistAsync("gr3");
        var patient = await SeedPatientAsync("gr3-p", "Nguyễn Văn A");

        var review = DentistReview.Create(dentist.Id, patient.Id, 4, "Bác sĩ tận tâm", ["Không đau", "Chuyên nghiệp"]);
        _db.DentistReviews.Add(review);
        await _db.SaveChangesAsync();

        var result = await _getHandler.Handle(new GetDentistReviewsQuery(dentist.Id), CancellationToken.None);

        var dto = result.Reviews.Should().ContainSingle().Subject;
        dto.PatientName.Should().Be("Nguyễn Văn A");
        dto.Rating.Should().Be(4);
        dto.Comment.Should().Be("Bác sĩ tận tâm");
        dto.Tags.Should().BeEquivalentTo(["Không đau", "Chuyên nghiệp"]);
    }

    // ---------- UpsertDentistReviewHandler ----------

    /// <summary>Điểm đánh giá ngoài khoảng 1-5 phải bị từ chối trước khi chạm tới bất kỳ repository nào.</summary>
    [TestCase(0)]
    [TestCase(6)]
    public async Task HandleAsync_InvalidRating_ThrowsValidationException(int invalidRating)
    {
        var command = new UpsertDentistReviewCommand(
            Guid.NewGuid(), Guid.NewGuid(), new CreateDentistReviewRequest(invalidRating, "Nội dung hợp lệ", null));

        Func<Task> act = () => _upsertHandler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Nội dung đánh giá rỗng/toàn khoảng trắng phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_EmptyComment_ThrowsValidationException()
    {
        var command = new UpsertDentistReviewCommand(
            Guid.NewGuid(), Guid.NewGuid(), new CreateDentistReviewRequest(5, "   ", null));

        Func<Task> act = () => _upsertHandler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>UserId không gắn với hồ sơ bệnh nhân nào phải báo NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_UserWithoutPatientProfile_ThrowsNotFoundException()
    {
        var dentist = await SeedDentistAsync("gr4");
        var command = new UpsertDentistReviewCommand(
            dentist.Id, Guid.NewGuid(), new CreateDentistReviewRequest(5, "Tốt", null));

        Func<Task> act = () => _upsertHandler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Bệnh nhân chưa từng hoàn tất buổi khám nào với nha sĩ này thì không được phép đánh giá.</summary>
    [Test]
    public async Task HandleAsync_PatientWithoutCompletedVisit_ThrowsValidationException()
    {
        var dentist = await SeedDentistAsync("gr5");
        var patient = await SeedPatientAsync("gr5-p");
        var command = new UpsertDentistReviewCommand(
            dentist.Id, patient.UserId, new CreateDentistReviewRequest(5, "Tốt", null));

        Func<Task> act = () => _upsertHandler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Bệnh nhân đã hoàn tất buổi khám và chưa từng đánh giá phải tạo mới được 1 bản ghi đánh giá.</summary>
    [Test]
    public async Task HandleAsync_NewReview_CreatesReviewSuccessfully()
    {
        var dentist = await SeedDentistAsync("gr6");
        var patient = await SeedPatientAsync("gr6-p", "Trần Thị B");
        await SeedCompletedVisitAsync(dentist.Id, patient.Id);

        var command = new UpsertDentistReviewCommand(
            dentist.Id, patient.UserId, new CreateDentistReviewRequest(5, "Rất hài lòng", ["Chuyên nghiệp"]));

        var result = await _upsertHandler.Handle(command, CancellationToken.None);

        result.PatientName.Should().Be("Trần Thị B");
        result.Rating.Should().Be(5);
        result.Comment.Should().Be("Rất hài lòng");
        result.Tags.Should().BeEquivalentTo(["Chuyên nghiệp"]);

        var stored = await _db.DentistReviews.SingleAsync(r => r.DentistId == dentist.Id && r.PatientId == patient.Id);
        stored.Rating.Should().Be(5);
        stored.Comment.Should().Be("Rất hài lòng");
    }

    /// <summary>Gửi đánh giá lần 2 cho cùng cặp nha sĩ/bệnh nhân phải cập nhật bản ghi cũ (upsert),
    /// không tạo thêm bản ghi mới.</summary>
    [Test]
    public async Task HandleAsync_ExistingReview_UpdatesInsteadOfCreatingDuplicate()
    {
        var dentist = await SeedDentistAsync("gr7");
        var patient = await SeedPatientAsync("gr7-p");
        await SeedCompletedVisitAsync(dentist.Id, patient.Id);

        var firstCommand = new UpsertDentistReviewCommand(
            dentist.Id, patient.UserId, new CreateDentistReviewRequest(3, "Bình thường", null));
        var firstResult = await _upsertHandler.Handle(firstCommand, CancellationToken.None);

        var secondCommand = new UpsertDentistReviewCommand(
            dentist.Id, patient.UserId, new CreateDentistReviewRequest(5, "Đã quay lại và rất hài lòng", ["Không đau"]));
        var secondResult = await _upsertHandler.Handle(secondCommand, CancellationToken.None);

        secondResult.Id.Should().Be(firstResult.Id);
        secondResult.Rating.Should().Be(5);
        secondResult.Comment.Should().Be("Đã quay lại và rất hài lòng");

        var allReviews = await _db.DentistReviews.Where(r => r.DentistId == dentist.Id && r.PatientId == patient.Id).ToListAsync();
        allReviews.Should().ContainSingle();
        allReviews[0].Rating.Should().Be(5);
    }
}
