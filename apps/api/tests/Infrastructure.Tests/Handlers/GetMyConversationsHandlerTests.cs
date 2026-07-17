using DentalClinic.API.Application.UseCases.Chat;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetMyConversationsHandlerTests
{
    private AppDbContext _db = null!;
    private IPatientRepository _patientRepo = null!;
    private GetMyConversationsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _patientRepo = Substitute.For<IPatientRepository>();
        _handler = new GetMyConversationsHandler(_patientRepo, _db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Tài khoản chưa có hồ sơ bệnh nhân phải trả về danh sách rỗng, không ném lỗi.</summary>
    [Test]
    public async Task HandleAsync_NoPatientProfile_ReturnsEmptyList()
    {
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Patient?)null);

        var result = await _handler.HandleAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    /// <summary>Danh sách cuộc trò chuyện phải sắp xếp theo lần cập nhật gần nhất trước.</summary>
    [Test]
    public async Task HandleAsync_MultipleConversations_OrderedByUpdatedAtDescending()
    {
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Patients.Add(patient);
        var older = ChatConversation.Create(patient.Id);
        var newer = ChatConversation.Create(patient.Id);
        newer.Touch();
        _db.ChatConversations.AddRange(older, newer);
        await _db.SaveChangesAsync();
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _handler.HandleAsync(Guid.NewGuid());

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(newer.Id);
    }

    /// <summary>Đoạn xem trước phải lấy nội dung tin nhắn cuối của người dùng (role "user"), không phải của bot.</summary>
    [Test]
    public async Task HandleAsync_PreviewUsesLastUserMessage_NotAssistantMessage()
    {
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Patients.Add(patient);
        var conversation = ChatConversation.Create(patient.Id);
        _db.ChatConversations.Add(conversation);
        await _db.SaveChangesAsync();
        _db.ChatMessages.Add(ChatMessage.Create(conversation.Id, "user", "Tôi muốn đặt lịch khám răng"));
        _db.ChatMessages.Add(ChatMessage.Create(conversation.Id, "assistant", "Vâng, bạn muốn khám vào lúc nào?"));
        await _db.SaveChangesAsync();
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _handler.HandleAsync(Guid.NewGuid());

        result[0].Preview.Should().Be("Tôi muốn đặt lịch khám răng");
    }

    /// <summary>Cuộc trò chuyện chưa có tin nhắn nào của người dùng phải hiện đoạn xem trước mặc định.</summary>
    [Test]
    public async Task HandleAsync_NoUserMessagesYet_UsesDefaultPreviewText()
    {
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Patients.Add(patient);
        var conversation = ChatConversation.Create(patient.Id);
        _db.ChatConversations.Add(conversation);
        await _db.SaveChangesAsync();
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _handler.HandleAsync(Guid.NewGuid());

        result[0].Preview.Should().Be("Cuộc trò chuyện mới");
    }

    /// <summary>Nội dung xem trước quá dài (trên 80 ký tự) phải bị cắt bớt và thêm dấu "…".</summary>
    [Test]
    public async Task HandleAsync_LongPreviewText_TruncatesTo80CharsWithEllipsis()
    {
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Patients.Add(patient);
        var conversation = ChatConversation.Create(patient.Id);
        _db.ChatConversations.Add(conversation);
        await _db.SaveChangesAsync();
        var longText = new string('a', 100);
        _db.ChatMessages.Add(ChatMessage.Create(conversation.Id, "user", longText));
        await _db.SaveChangesAsync();
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _handler.HandleAsync(Guid.NewGuid());

        result[0].Preview.Should().HaveLength(81); // 80 ký tự + dấu "…"
        result[0].Preview.Should().EndWith("…");
    }
}
