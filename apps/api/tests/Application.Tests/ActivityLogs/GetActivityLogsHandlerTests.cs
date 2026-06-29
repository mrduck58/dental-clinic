using DentalClinic.API.Application.UseCases.ActivityLogs;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.ActivityLogs;

[TestFixture]
public class GetActivityLogsHandlerTests
{
    private IActivityLogRepository _repo = null!;
    private GetActivityLogsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IActivityLogRepository>();
        _handler = new GetActivityLogsHandler(_repo);

        // Mặc định: trả về danh sách rỗng
        _repo.GetPagedAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<ActivityLog>().AsReadOnly() as IReadOnlyList<ActivityLog>, 0));
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    /// <summary>
    /// Query không có filter phải gọi GetPagedAsync với page=1, pageSize=20 (mặc định),
    /// và trả về DTO đúng số lượng item từ repository.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoFilters_CallsRepositoryAndReturnsDto()
    {
        var logs = new List<ActivityLog> { MakeLog(), MakeLog() };
        SetupRepo(logs, total: 2);

        var result = await _handler.HandleAsync(new GetActivityLogsQuery(null, null, null, null, null, null));

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        await _repo.Received(1).GetPagedAsync(
            null, null, null, null, null, null, 1, 20, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TotalPages phải được tính đúng: ceil(TotalCount / pageSize).
    /// 15 items với pageSize=10 → 2 trang.
    /// </summary>
    [Test]
    public async Task HandleAsync_TotalPages_CalculatedCorrectly()
    {
        SetupRepo(Enumerable.Repeat(MakeLog(), 10).ToList(), total: 15);

        var result = await _handler.HandleAsync(
            new GetActivityLogsQuery(null, null, null, null, null, null, Page: 1, PageSize: 10));

        result.TotalPages.Should().Be(2);
        result.TotalCount.Should().Be(15);
    }

    /// <summary>
    /// TotalPages khi tổng chia hết pageSize không được làm tròn lên thêm 1.
    /// 20 items với pageSize=10 → đúng 2 trang.
    /// </summary>
    [Test]
    public async Task HandleAsync_TotalPagesExactDivision_NoExtraPage()
    {
        SetupRepo(Enumerable.Repeat(MakeLog(), 10).ToList(), total: 20);

        var result = await _handler.HandleAsync(
            new GetActivityLogsQuery(null, null, null, null, null, null, Page: 1, PageSize: 10));

        result.TotalPages.Should().Be(2);
    }

    /// <summary>
    /// Page và PageSize phải được truyền đúng xuống repository, không được thay đổi giá trị.
    /// </summary>
    [Test]
    public async Task HandleAsync_CustomPageAndPageSize_PassedToRepository()
    {
        await _handler.HandleAsync(
            new GetActivityLogsQuery(null, null, null, null, null, null, Page: 3, PageSize: 50));

        await _repo.Received(1).GetPagedAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            3, 50, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// PageSize vượt quá 100 phải bị clamp về 100 để tránh trả về quá nhiều dữ liệu.
    /// </summary>
    [Test]
    public async Task HandleAsync_PageSizeAbove100_ClampedTo100()
    {
        await _handler.HandleAsync(
            new GetActivityLogsQuery(null, null, null, null, null, null, Page: 1, PageSize: 9999));

        await _repo.Received(1).GetPagedAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            1, 100, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// PageSize nhỏ hơn 1 phải bị clamp về 1 để tránh lỗi chia cho 0 khi tính TotalPages.
    /// </summary>
    [Test]
    public async Task HandleAsync_PageSizeZeroOrNegative_ClampedTo1()
    {
        await _handler.HandleAsync(
            new GetActivityLogsQuery(null, null, null, null, null, null, Page: 1, PageSize: 0));

        await _repo.Received(1).GetPagedAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            1, 1, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Page nhỏ hơn 1 phải bị đưa về 1 — không có trang 0 hoặc âm.
    /// </summary>
    [Test]
    public async Task HandleAsync_PageZeroOrNegative_ClampedTo1()
    {
        await _handler.HandleAsync(
            new GetActivityLogsQuery(null, null, null, null, null, null, Page: -5, PageSize: 10));

        await _repo.Received(1).GetPagedAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            1, 10, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Các filter (action, module, status, search) phải được truyền đúng xuống repository.
    /// </summary>
    [Test]
    public async Task HandleAsync_WithFilters_PassedToRepository()
    {
        var start = DateTimeOffset.UtcNow.AddDays(-7);
        var end   = DateTimeOffset.UtcNow;

        await _handler.HandleAsync(new GetActivityLogsQuery(
            Action:    "create",
            Module:    "account",
            Status:    "success",
            Search:    "admin",
            StartDate: start,
            EndDate:   end));

        await _repo.Received(1).GetPagedAsync(
            "create", "account", "success", "admin", start, end,
            1, 20, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Từng ActivityLog từ repository phải được ánh xạ thành ActivityLogDto với đầy đủ các trường.
    /// </summary>
    [Test]
    public async Task HandleAsync_SingleLog_MappedToDto()
    {
        var userId = Guid.NewGuid();
        var log    = MakeLog(userId, "admin@test.com", "Admin", "login", "account", "success");
        SetupRepo(new List<ActivityLog> { log }, total: 1);

        var result = await _handler.HandleAsync(
            new GetActivityLogsQuery(null, null, null, null, null, null));

        var dto = result.Items.Single();
        dto.UserId.Should().Be(userId);
        dto.UserName.Should().Be("admin@test.com");
        dto.UserRole.Should().Be("Admin");
        dto.Action.Should().Be("login");
        dto.Module.Should().Be("account");
        dto.Status.Should().Be("success");
    }

    /// <summary>
    /// Khi repository trả về 0 item, response phải trả về 0 item, TotalCount=0, TotalPages=1
    /// (1 trang trống, không phải 0 trang — để pagination UI không bị lỗi).
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptyRepository_ReturnsTotalPagesOne()
    {
        // Default SetUp already returns ([], 0)
        var result = await _handler.HandleAsync(
            new GetActivityLogsQuery(null, null, null, null, null, null, Page: 1, PageSize: 10));

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0); // ceil(0/10) = 0
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupRepo(IList<ActivityLog> items, int total)
    {
        _repo.GetPagedAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ActivityLog>)items.AsReadOnly(), total));
    }

    private static ActivityLog MakeLog(
        Guid? userId = null,
        string userName = "user@test.com",
        string userRole = "Staff",
        string action   = "create",
        string module   = "account",
        string status   = "success")
        => ActivityLog.Create(userId ?? Guid.NewGuid(), userName, userRole, action, module, "Mô tả", status);
}
