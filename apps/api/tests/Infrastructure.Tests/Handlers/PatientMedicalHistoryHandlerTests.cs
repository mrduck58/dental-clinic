using DentalClinic.API.Application.DTOs.Patients;
using DentalClinic.API.Application.UseCases.Patients;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

/// <summary>
/// PatientsController trước đây có logic tra cứu/kiểm tra quyền lặp lại ở nhiều action;
/// đã tách thành PatientAccessHelper (dùng chung) + các handler MediatR cho hồ sơ bệnh án
/// (chính chủ và thành viên gia đình). File này bao phủ PatientAccessHelper,
/// SearchPatientsHandler và 4 handler tiền sử bệnh lý.
/// </summary>
[TestFixture]
public class PatientMedicalHistoryHandlerTests
{
    private AppDbContext _db = null!;
    private IPatientRepository _patientRepository = null!;
    private IUserRepository _userRepository = null!;
    private PatientAccessHelper _patientAccess = null!;

    private IPatientRepository _searchPatientRepo = null!;
    private SearchPatientsHandler _search = null!;

    private GetMyMedicalHistoryHandler _getMyHistory = null!;
    private UpdateMyMedicalHistoryHandler _updateMyHistory = null!;
    private GetFamilyMemberMedicalHistoryHandler _getFamilyHistory = null!;
    private UpdateFamilyMemberMedicalHistoryHandler _updateFamilyHistory = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _patientRepository = new PatientRepository(_db);
        _userRepository = new UserRepository(_db);
        _patientAccess = new PatientAccessHelper(_patientRepository, _userRepository);

        _searchPatientRepo = Substitute.For<IPatientRepository>();
        _search = new SearchPatientsHandler(_searchPatientRepo);

        _getMyHistory = new GetMyMedicalHistoryHandler(_patientAccess);
        _updateMyHistory = new UpdateMyMedicalHistoryHandler(_patientAccess, _patientRepository);
        _getFamilyHistory = new GetFamilyMemberMedicalHistoryHandler(_patientAccess, _patientRepository);
        _updateFamilyHistory = new UpdateFamilyMemberMedicalHistoryHandler(_patientAccess, _patientRepository);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(User User, Patient Patient)> SeedPatientAsync(
        string fullName = "Nguyen Van A", string? gender = "Nam", string? phone = "0900000001", bool hasAccount = true)
    {
        var user = hasAccount
            ? User.Create($"user-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", UserRole.Patient, phone, fullName)
            : User.CreateEmployee($"{Guid.NewGuid()}@test.com", UserRole.Patient, phone, fullName);
        user.UpdateGender(gender);
        _db.Users.Add(user);
        var patient = Patient.Create(user.Id, new DateOnly(1990, 1, 1));
        patient.User = user;
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return (user, patient);
    }

    private async Task<(User User, Patient Patient)> SeedFamilyMemberAsync(
        Guid primaryPatientId, string fullName = "Nguyen Van B", string relationship = "Con")
    {
        var user = User.CreateEmployee($"{Guid.NewGuid()}@test.com", UserRole.Patient, null, fullName);
        _db.Users.Add(user);
        var member = Patient.Create(user.Id, new DateOnly(2015, 1, 1), primaryPatientId: primaryPatientId, relationship: relationship);
        member.User = user;
        _db.Patients.Add(member);
        await _db.SaveChangesAsync();
        return (user, member);
    }

    // ---------- PatientAccessHelper ----------

    /// <summary>User đã có hồ sơ Patient sẵn phải trả về đúng hồ sơ đó, không tạo mới.</summary>
    [Test]
    public async Task GetOrCreatePrimaryPatientAsync_ExistingPatient_ReturnsExistingPatient()
    {
        var (user, patient) = await SeedPatientAsync();

        var result = await _patientAccess.GetOrCreatePrimaryPatientAsync(user.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(patient.Id);
        (await _db.Patients.CountAsync()).Should().Be(1);
    }

    /// <summary>User có tài khoản nhưng chưa từng có hồ sơ Patient phải được tự động khởi tạo hồ sơ mới.</summary>
    [Test]
    public async Task GetOrCreatePrimaryPatientAsync_UserWithoutPatientProfile_CreatesNewPatient()
    {
        var user = User.Create("user-x", $"{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _patientAccess.GetOrCreatePrimaryPatientAsync(user.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.Id);
        (await _db.Patients.CountAsync(p => p.UserId == user.Id)).Should().Be(1);
    }

    /// <summary>Không tồn tại User ứng với userId thì phải trả về null thay vì tạo hồ sơ mồ côi.</summary>
    [Test]
    public async Task GetOrCreatePrimaryPatientAsync_UserNotFound_ReturnsNull()
    {
        var result = await _patientAccess.GetOrCreatePrimaryPatientAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    /// <summary>So sánh cùng một hồ sơ (chính chủ) phải được coi là có quyền truy cập.</summary>
    [Test]
    public void IsSelfOrFamilyMember_SamePatientId_ReturnsTrue()
    {
        var patient = Patient.Create(Guid.NewGuid());

        PatientAccessHelper.IsSelfOrFamilyMember(patient, patient).Should().BeTrue();
    }

    /// <summary>Hồ sơ có PrimaryPatientId trỏ về hồ sơ chính chủ phải được coi là thành viên gia đình hợp lệ.</summary>
    [Test]
    public void IsSelfOrFamilyMember_FamilyMemberOfPrimary_ReturnsTrue()
    {
        var primary = Patient.Create(Guid.NewGuid());
        var member = Patient.Create(Guid.NewGuid(), primaryPatientId: primary.Id, relationship: "Con");

        PatientAccessHelper.IsSelfOrFamilyMember(member, primary).Should().BeTrue();
    }

    /// <summary>Hồ sơ không liên quan (không phải chính chủ, không phải thành viên gia đình) phải bị từ chối.</summary>
    [Test]
    public void IsSelfOrFamilyMember_UnrelatedPatient_ReturnsFalse()
    {
        var primary = Patient.Create(Guid.NewGuid());
        var stranger = Patient.Create(Guid.NewGuid());

        PatientAccessHelper.IsSelfOrFamilyMember(stranger, primary).Should().BeFalse();
    }

    // ---------- SearchPatientsHandler ----------

    /// <summary>Từ khóa null phải được coi là chuỗi rỗng và trả về danh sách rỗng, không gọi repository.</summary>
    [Test]
    public async Task Handle_SearchQueryIsNull_ReturnsEmptyWithoutQueryingRepository()
    {
        var result = await _search.Handle(new SearchPatientsQuery(null, 10), CancellationToken.None);

        result.Should().BeEmpty();
        await _searchPatientRepo.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Từ khóa dưới 2 ký tự phải trả về rỗng để tránh quét toàn bảng.</summary>
    [Test]
    public async Task Handle_SearchQueryTooShort_ReturnsEmpty()
    {
        var result = await _search.Handle(new SearchPatientsQuery("a", 10), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>OnlyWithoutAccount=true phải bỏ qua ràng buộc tối thiểu 2 ký tự — dùng để duyệt
    /// danh sách bệnh nhân chưa có tài khoản mà không cần gõ từ khóa.</summary>
    [Test]
    public async Task Handle_OnlyWithoutAccountTrue_BypassesMinLengthAndQueriesRepository()
    {
        _searchPatientRepo.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _search.Handle(new SearchPatientsQuery(null, 10, OnlyWithoutAccount: true), CancellationToken.None);

        result.Should().BeEmpty();
        await _searchPatientRepo.Received(1).SearchAsync("", 10, true, Arg.Any<CancellationToken>());
    }

    /// <summary>Limit không hợp lệ (<= 0) phải được thay bằng giá trị mặc định 8 khi gọi repository.</summary>
    [Test]
    public async Task Handle_LimitZeroOrNegative_ClampsToDefaultEight()
    {
        _searchPatientRepo.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _search.Handle(new SearchPatientsQuery("Nguyen", 0), CancellationToken.None);

        await _searchPatientRepo.Received(1).SearchAsync("Nguyen", 8, false, Arg.Any<CancellationToken>());
    }

    /// <summary>Limit vượt quá 20 phải được thay bằng giá trị mặc định 8 khi gọi repository.</summary>
    [Test]
    public async Task Handle_LimitAboveTwenty_ClampsToDefaultEight()
    {
        _searchPatientRepo.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _search.Handle(new SearchPatientsQuery("Nguyen", 100), CancellationToken.None);

        await _searchPatientRepo.Received(1).SearchAsync("Nguyen", 8, false, Arg.Any<CancellationToken>());
    }

    /// <summary>Limit hợp lệ (1-20) phải được truyền nguyên vẹn xuống repository.</summary>
    [Test]
    public async Task Handle_LimitWithinRange_PassesThroughUnchanged()
    {
        _searchPatientRepo.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _search.Handle(new SearchPatientsQuery("Nguyen", 5), CancellationToken.None);

        await _searchPatientRepo.Received(1).SearchAsync("Nguyen", 5, false, Arg.Any<CancellationToken>());
    }

    /// <summary>Kết quả trả về phải map đúng các trường, và HasAccount phải phân biệt bệnh nhân có
    /// tài khoản đăng nhập (PasswordHash khác null) với bệnh nhân chỉ tạo tại quầy.</summary>
    [Test]
    public async Task Handle_MatchingPatients_MapsHasAccountCorrectly()
    {
        var withAccount = User.Create("acc1", "acc1@test.com", "hash", UserRole.Patient, "0911111111", "Co Tai Khoan");
        var patientWithAccount = Patient.Create(withAccount.Id);
        patientWithAccount.User = withAccount;

        var withoutAccount = User.CreateEmployee("walkin@test.com", UserRole.Patient, "0922222222", "Khong Tai Khoan");
        var patientWithoutAccount = Patient.Create(withoutAccount.Id);
        patientWithoutAccount.User = withoutAccount;

        _searchPatientRepo.SearchAsync("nguyen", 8, false, Arg.Any<CancellationToken>())
            .Returns([patientWithAccount, patientWithoutAccount]);

        var result = (await _search.Handle(new SearchPatientsQuery("nguyen", 8), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result[0].HasAccount.Should().BeTrue();
        result[0].PhoneNumber.Should().Be("0911111111");
        result[1].HasAccount.Should().BeFalse();
        result[1].PhoneNumber.Should().Be("0922222222");
    }

    // ---------- GetMyMedicalHistoryHandler ----------

    /// <summary>Bệnh nhân đã có hồ sơ phải trả về đúng tiền sử bệnh lý đã lưu.</summary>
    [Test]
    public async Task GetMyMedicalHistory_ExistingPatient_ReturnsStoredHistory()
    {
        var (user, patient) = await SeedPatientAsync();
        patient.UpdateMedicalHistory("Dị ứng Penicillin");
        await _db.SaveChangesAsync();

        var result = await _getMyHistory.Handle(new GetMyMedicalHistoryQuery(user.Id), CancellationToken.None);

        result.Should().Be("Dị ứng Penicillin");
    }

    /// <summary>UserId không tồn tại thì phải báo NotFoundException thay vì trả về null lặng lẽ.</summary>
    [Test]
    public async Task GetMyMedicalHistory_UserNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _getMyHistory.Handle(new GetMyMedicalHistoryQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- UpdateMyMedicalHistoryHandler ----------

    /// <summary>Cập nhật tiền sử bệnh lý hợp lệ phải được lưu lại vào hồ sơ chính chủ.</summary>
    [Test]
    public async Task UpdateMyMedicalHistory_ValidRequest_PersistsNewHistory()
    {
        var (user, patient) = await SeedPatientAsync();

        await _updateMyHistory.Handle(new UpdateMyMedicalHistoryCommand(user.Id, "Tiểu đường type 2"), CancellationToken.None);

        var updated = await _db.Patients.FindAsync(patient.Id);
        updated!.MedicalHistory.Should().Be("Tiểu đường type 2");
    }

    /// <summary>Truyền MedicalHistory null phải được chuẩn hóa thành chuỗi rỗng thay vì giữ null.</summary>
    [Test]
    public async Task UpdateMyMedicalHistory_NullHistory_NormalizesToEmptyString()
    {
        var (user, patient) = await SeedPatientAsync();

        await _updateMyHistory.Handle(new UpdateMyMedicalHistoryCommand(user.Id, null), CancellationToken.None);

        var updated = await _db.Patients.FindAsync(patient.Id);
        updated!.MedicalHistory.Should().BeEmpty();
    }

    /// <summary>UserId không tồn tại thì phải báo NotFoundException, không được âm thầm bỏ qua.</summary>
    [Test]
    public async Task UpdateMyMedicalHistory_UserNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _updateMyHistory.Handle(
            new UpdateMyMedicalHistoryCommand(Guid.NewGuid(), "x"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- GetFamilyMemberMedicalHistoryHandler ----------

    /// <summary>Xem tiền sử bệnh lý của thành viên gia đình hợp lệ phải trả về đúng dữ liệu.</summary>
    [Test]
    public async Task GetFamilyMemberMedicalHistory_ValidFamilyMember_ReturnsHistory()
    {
        var (primaryUser, primaryPatient) = await SeedPatientAsync();
        var (_, member) = await SeedFamilyMemberAsync(primaryPatient.Id);
        member.UpdateMedicalHistory("Hen suyễn");
        await _db.SaveChangesAsync();

        var result = await _getFamilyHistory.Handle(
            new GetFamilyMemberMedicalHistoryQuery(primaryUser.Id, member.Id), CancellationToken.None);

        result.Should().Be("Hen suyễn");
    }

    /// <summary>PatientId không tồn tại phải báo NotFoundException.</summary>
    [Test]
    public async Task GetFamilyMemberMedicalHistory_PatientNotFound_ThrowsNotFoundException()
    {
        var (primaryUser, _) = await SeedPatientAsync();

        Func<Task> act = () => _getFamilyHistory.Handle(
            new GetFamilyMemberMedicalHistoryQuery(primaryUser.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Cố xem hồ sơ của một bệnh nhân không phải chính mình và không phải thành viên gia đình
    /// phải bị từ chối bằng ForbiddenException.</summary>
    [Test]
    public async Task GetFamilyMemberMedicalHistory_UnrelatedPatient_ThrowsForbiddenException()
    {
        var (primaryUser, _) = await SeedPatientAsync();
        var (_, stranger) = await SeedPatientAsync("Nguoi La", "Nu", "0933333333");

        Func<Task> act = () => _getFamilyHistory.Handle(
            new GetFamilyMemberMedicalHistoryQuery(primaryUser.Id, stranger.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    /// <summary>UserId của người gọi không tồn tại thì phải báo NotFoundException ngay từ bước lấy hồ sơ chính chủ.</summary>
    [Test]
    public async Task GetFamilyMemberMedicalHistory_CallerUserNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _getFamilyHistory.Handle(
            new GetFamilyMemberMedicalHistoryQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- UpdateFamilyMemberMedicalHistoryHandler ----------

    /// <summary>Cập nhật tiền sử bệnh lý của thành viên gia đình hợp lệ phải được lưu lại.</summary>
    [Test]
    public async Task UpdateFamilyMemberMedicalHistory_ValidFamilyMember_PersistsHistory()
    {
        var (primaryUser, primaryPatient) = await SeedPatientAsync();
        var (_, member) = await SeedFamilyMemberAsync(primaryPatient.Id);

        await _updateFamilyHistory.Handle(
            new UpdateFamilyMemberMedicalHistoryCommand(primaryUser.Id, member.Id, "Viêm xoang mạn tính"), CancellationToken.None);

        var updated = await _db.Patients.FindAsync(member.Id);
        updated!.MedicalHistory.Should().Be("Viêm xoang mạn tính");
    }

    /// <summary>Sửa hồ sơ của một bệnh nhân không liên quan phải bị từ chối bằng ForbiddenException.</summary>
    [Test]
    public async Task UpdateFamilyMemberMedicalHistory_UnrelatedPatient_ThrowsForbiddenException()
    {
        var (primaryUser, _) = await SeedPatientAsync();
        var (_, stranger) = await SeedPatientAsync("Nguoi La", "Nu", "0944444444");

        Func<Task> act = () => _updateFamilyHistory.Handle(
            new UpdateFamilyMemberMedicalHistoryCommand(primaryUser.Id, stranger.Id, "x"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    /// <summary>PatientId không tồn tại phải báo NotFoundException.</summary>
    [Test]
    public async Task UpdateFamilyMemberMedicalHistory_PatientNotFound_ThrowsNotFoundException()
    {
        var (primaryUser, _) = await SeedPatientAsync();

        Func<Task> act = () => _updateFamilyHistory.Handle(
            new UpdateFamilyMemberMedicalHistoryCommand(primaryUser.Id, Guid.NewGuid(), "x"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
