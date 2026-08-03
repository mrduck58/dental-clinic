using DentalClinic.API.Application.UseCases.Patients;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

/// <summary>
/// Bao phủ 4 handler CRUD thành viên gia đình (GetFamilyMembers/CreateFamilyMember/
/// UpdateFamilyMember/DeleteFamilyMember) được tách ra từ PatientsController.
/// </summary>
[TestFixture]
public class FamilyMemberHandlerTests
{
    private AppDbContext _db = null!;
    private IPatientRepository _patientRepository = null!;
    private IUserRepository _userRepository = null!;
    private PatientAccessHelper _patientAccess = null!;

    private GetFamilyMembersHandler _getMembers = null!;
    private CreateFamilyMemberHandler _create = null!;
    private UpdateFamilyMemberHandler _update = null!;
    private DeleteFamilyMemberHandler _delete = null!;

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

        _getMembers = new GetFamilyMembersHandler(_patientAccess, _patientRepository);
        _create = new CreateFamilyMemberHandler(_patientAccess, _patientRepository, _userRepository);
        _update = new UpdateFamilyMemberHandler(_patientAccess, _patientRepository);
        _delete = new DeleteFamilyMemberHandler(_patientAccess, _patientRepository);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(User User, Patient Patient)> SeedPrimaryPatientAsync(
        string fullName = "Nguyen Van A", string? phone = "0900000001")
    {
        var user = User.Create($"user-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", "Patient", phone, fullName);
        user.UpdateGender("Nam");
        _db.Users.Add(user);
        var patient = Patient.Create(user.Id, new DateOnly(1985, 5, 20));
        patient.User = user;
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return (user, patient);
    }

    private async Task<(User User, Patient Patient)> SeedFamilyMemberAsync(
        Guid primaryPatientId, string fullName = "Nguyen Van B", string relationship = "Con",
        string gender = "Nu", string? phone = "0900000002")
    {
        var user = User.CreateEmployee($"{Guid.NewGuid()}@test.com", "Patient", phone, fullName);
        user.UpdateGender(gender);
        _db.Users.Add(user);
        var member = Patient.Create(
            user.Id, new DateOnly(2015, 3, 10), primaryPatientId: primaryPatientId, relationship: relationship);
        member.User = user;
        _db.Patients.Add(member);
        await _db.SaveChangesAsync();
        return (user, member);
    }

    // ---------- GetFamilyMembersHandler ----------

    /// <summary>Chưa có thành viên gia đình nào phải trả về danh sách rỗng, không lỗi.</summary>
    [Test]
    public async Task GetFamilyMembers_NoMembers_ReturnsEmptyList()
    {
        var (primaryUser, _) = await SeedPrimaryPatientAsync();

        var result = await _getMembers.Handle(new GetFamilyMembersQuery(primaryUser.Id), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Có thành viên gia đình phải trả về đầy đủ, map đúng các trường DTO.</summary>
    [Test]
    public async Task GetFamilyMembers_HasMembers_ReturnsMappedDtos()
    {
        var (primaryUser, primaryPatient) = await SeedPrimaryPatientAsync();
        await SeedFamilyMemberAsync(primaryPatient.Id, "Con Gai", "Con", "Nu", "0911111111");

        var result = (await _getMembers.Handle(new GetFamilyMembersQuery(primaryUser.Id), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
        result[0].FullName.Should().Be("Con Gai");
        result[0].Relationship.Should().Be("Con");
        result[0].Gender.Should().Be("Nu");
        result[0].PhoneNumber.Should().Be("0911111111");
    }

    /// <summary>Danh sách thành viên gia đình không được lẫn hồ sơ của người dùng khác không liên quan.</summary>
    [Test]
    public async Task GetFamilyMembers_UnrelatedPatientExists_NotIncludedInResult()
    {
        var (primaryUser, primaryPatient) = await SeedPrimaryPatientAsync();
        await SeedFamilyMemberAsync(primaryPatient.Id, "Con Trai");
        await SeedPrimaryPatientAsync("Nguoi Khac", "0955555555"); // primary patient không liên quan

        var result = (await _getMembers.Handle(new GetFamilyMembersQuery(primaryUser.Id), CancellationToken.None)).ToList();

        result.Should().ContainSingle(m => m.FullName == "Con Trai");
    }

    /// <summary>UserId của người gọi không tồn tại phải báo NotFoundException.</summary>
    [Test]
    public async Task GetFamilyMembers_CallerUserNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _getMembers.Handle(new GetFamilyMembersQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- CreateFamilyMemberHandler ----------

    /// <summary>Thêm thành viên gia đình hợp lệ phải tạo user placeholder (chưa có tài khoản đăng nhập)
    /// và một hồ sơ Patient mới liên kết PrimaryPatientId về đúng chính chủ.</summary>
    [Test]
    public async Task CreateFamilyMember_ValidRequest_PersistsMemberLinkedToPrimary()
    {
        var (primaryUser, primaryPatient) = await SeedPrimaryPatientAsync();
        var command = new CreateFamilyMemberCommand(
            primaryUser.Id, "Con Ut", "Con", new DateOnly(2018, 6, 15), "Nam", "0966666666", "http://pic.jpg");

        var dto = await _create.Handle(command, CancellationToken.None);

        var persisted = await _db.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == dto.Id);
        persisted.Should().NotBeNull();
        persisted!.PrimaryPatientId.Should().Be(primaryPatient.Id);
        persisted.Relationship.Should().Be("Con");
        persisted.DateOfBirth.Should().Be(new DateOnly(2018, 6, 15));
        persisted.ProfilePictureUrl.Should().Be("http://pic.jpg");
        persisted.User.Should().NotBeNull();
        persisted.User.PasswordHash.Should().BeNull("thành viên gia đình chỉ là hồ sơ placeholder, chưa có tài khoản đăng nhập riêng");
        persisted.User.Role.Should().Be("Patient");
    }

    /// <summary>Tạo thành viên gia đình mới không được ảnh hưởng tới các thành viên đã có trước đó.</summary>
    [Test]
    public async Task CreateFamilyMember_ValidRequest_DoesNotAffectOtherExistingMembers()
    {
        var (primaryUser, primaryPatient) = await SeedPrimaryPatientAsync();
        await SeedFamilyMemberAsync(primaryPatient.Id, "Con Dau");

        await _create.Handle(
            new CreateFamilyMemberCommand(primaryUser.Id, "Con Thu Hai", "Con", null, "Nu", null, null),
            CancellationToken.None);

        var members = await _patientRepository.GetFamilyMembersAsync(primaryPatient.Id, CancellationToken.None);
        members.Should().HaveCount(2);
    }

    /// <summary>UserId của người gọi không tồn tại phải báo NotFoundException, không tạo dữ liệu mồ côi.</summary>
    [Test]
    public async Task CreateFamilyMember_CallerUserNotFound_ThrowsNotFoundException()
    {
        var command = new CreateFamilyMemberCommand(Guid.NewGuid(), "X", "Con", null, "Nam", null, null);

        Func<Task> act = () => _create.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        (await _db.Patients.CountAsync()).Should().Be(0);
        (await _db.Users.CountAsync()).Should().Be(0);
    }

    // ---------- UpdateFamilyMemberHandler ----------

    /// <summary>Cập nhật thông tin thành viên gia đình hợp lệ phải ghi đè đúng các trường mới,
    /// bao gồm cả các trường được ủy quyền qua User (họ tên, giới tính, SĐT).</summary>
    [Test]
    public async Task UpdateFamilyMember_ValidRequest_UpdatesAllFields()
    {
        var (primaryUser, primaryPatient) = await SeedPrimaryPatientAsync();
        var (_, member) = await SeedFamilyMemberAsync(primaryPatient.Id);
        var command = new UpdateFamilyMemberCommand(
            primaryUser.Id, member.Id, "Ten Moi", "Vo/Chong", new DateOnly(1995, 2, 2), "Nu", "0977777777", "http://new.jpg");

        await _update.Handle(command, CancellationToken.None);

        var updated = await _db.Patients.Include(p => p.User).FirstAsync(p => p.Id == member.Id);
        updated.FullName.Should().Be("Ten Moi");
        updated.Gender.Should().Be("Nu");
        updated.PhoneNumber.Should().Be("0977777777");
        updated.DateOfBirth.Should().Be(new DateOnly(1995, 2, 2));
        updated.Relationship.Should().Be("Vo/Chong");
        updated.ProfilePictureUrl.Should().Be("http://new.jpg");
        updated.PrimaryPatientId.Should().Be(primaryPatient.Id);
    }

    /// <summary>Sửa một thành viên không tồn tại phải báo NotFoundException.</summary>
    [Test]
    public async Task UpdateFamilyMember_MemberNotFound_ThrowsNotFoundException()
    {
        var (primaryUser, _) = await SeedPrimaryPatientAsync();
        var command = new UpdateFamilyMemberCommand(
            primaryUser.Id, Guid.NewGuid(), "X", "Con", null, "Nam", null, null);

        Func<Task> act = () => _update.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Không được sửa hồ sơ thành viên gia đình thuộc về một chính chủ khác — phải báo
    /// NotFoundException (che giấu sự tồn tại của hồ sơ đó thay vì lộ ra bằng Forbidden).</summary>
    [Test]
    public async Task UpdateFamilyMember_MemberBelongsToDifferentPrimary_ThrowsNotFoundException()
    {
        var (primaryUser, _) = await SeedPrimaryPatientAsync();
        var (_, otherPrimaryPatient) = await SeedPrimaryPatientAsync("Chinh Chu Khac", "0988888888");
        var (_, otherMember) = await SeedFamilyMemberAsync(otherPrimaryPatient.Id, "Con Cua Nguoi Khac");

        var command = new UpdateFamilyMemberCommand(
            primaryUser.Id, otherMember.Id, "Hack", "Con", null, "Nam", null, null);

        Func<Task> act = () => _update.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>UserId của người gọi không tồn tại phải báo NotFoundException.</summary>
    [Test]
    public async Task UpdateFamilyMember_CallerUserNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateFamilyMemberCommand(
            Guid.NewGuid(), Guid.NewGuid(), "X", "Con", null, "Nam", null, null);

        Func<Task> act = () => _update.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- DeleteFamilyMemberHandler ----------

    /// <summary>Xóa thành viên gia đình hợp lệ phải loại bỏ khỏi DB.</summary>
    [Test]
    public async Task DeleteFamilyMember_ValidRequest_RemovesFromDb()
    {
        var (primaryUser, primaryPatient) = await SeedPrimaryPatientAsync();
        var (_, member) = await SeedFamilyMemberAsync(primaryPatient.Id);

        await _delete.Handle(new DeleteFamilyMemberCommand(primaryUser.Id, member.Id), CancellationToken.None);

        (await _db.Patients.FindAsync(member.Id)).Should().BeNull();
    }

    /// <summary>Xóa một thành viên không tồn tại phải báo NotFoundException.</summary>
    [Test]
    public async Task DeleteFamilyMember_MemberNotFound_ThrowsNotFoundException()
    {
        var (primaryUser, _) = await SeedPrimaryPatientAsync();

        Func<Task> act = () => _delete.Handle(
            new DeleteFamilyMemberCommand(primaryUser.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Không được xóa hồ sơ thành viên gia đình thuộc về một chính chủ khác.</summary>
    [Test]
    public async Task DeleteFamilyMember_MemberBelongsToDifferentPrimary_ThrowsNotFoundException()
    {
        var (primaryUser, _) = await SeedPrimaryPatientAsync();
        var (_, otherPrimaryPatient) = await SeedPrimaryPatientAsync("Chinh Chu Khac 2", "0999999999");
        var (_, otherMember) = await SeedFamilyMemberAsync(otherPrimaryPatient.Id, "Con Cua Nguoi Khac 2");

        Func<Task> act = () => _delete.Handle(
            new DeleteFamilyMemberCommand(primaryUser.Id, otherMember.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        (await _db.Patients.FindAsync(otherMember.Id)).Should().NotBeNull();
    }
}
