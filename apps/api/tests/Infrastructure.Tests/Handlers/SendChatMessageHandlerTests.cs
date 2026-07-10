using DentalClinic.API.Application.UseCases.Chat;
using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class SendChatMessageHandlerTests
{
    private AppDbContext _db = null!;
    private IPatientRepository _patientRepo = null!;
    private IClinicInfoRepository _clinicInfoRepo = null!;
    private IUserRepository _userRepo = null!;
    private IAiChatService _aiChatService = null!;
    private SendChatMessageHandler _handler = null!;
    private Guid _userId;
    private ChatConversation _conversation = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        var user = User.Create($"patient-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", "Patient", fullName: "Bệnh nhân Test");
        _db.Users.Add(user);
        _userId = user.Id;

        var patient = Patient.Create("Bệnh nhân Test", new DateOnly(1990, 1, 1), "Nam", user.Id);
        _db.Patients.Add(patient);

        _conversation = ChatConversation.Create(patient.Id);
        _db.ChatConversations.Add(_conversation);

        await _db.SaveChangesAsync();

        _patientRepo = Substitute.For<IPatientRepository>();
        _patientRepo.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(patient);

        _clinicInfoRepo = Substitute.For<IClinicInfoRepository>();
        _clinicInfoRepo.GetAsync(Arg.Any<CancellationToken>()).Returns((DentalClinic.API.Domain.Entities.ClinicInfo?)null);

        _userRepo = Substitute.For<IUserRepository>();
        _userRepo.GetStaffPagedAsync(null, null, null, 1, 500, Arg.Any<CancellationToken>())
            .Returns((new List<User>(), 0));

        _aiChatService = Substitute.For<IAiChatService>();
        _aiChatService.AskAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiChatReply("Câu trả lời test", false));

        var getDentistsHandler = new GetDentistsHandler(_userRepo);
        _handler = new SendChatMessageHandler(_patientRepo, _clinicInfoRepo, getDentistsHandler, _aiChatService, _db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>
    /// GetPromotionsHandler không tự lọc IsActive/khoảng ngày, nên SendChatMessageHandler phải tự lọc khi
    /// build snapshot — ưu đãi đã hết hạn không được xuất hiện trong system instruction gửi cho Gemini,
    /// nếu không chatbot có thể tư vấn sai một ưu đãi không còn áp dụng.
    /// </summary>
    [Test]
    public async Task HandleAsync_OnlyActivePromotionInSnapshot_AndPersistsMessagePair()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activePromo = Promotion.Create(
            "HE2026", "Giảm 10% dịp hè", "Ưu đãi mùa hè năm nay",
            "Percentage", 10, [],
            today.AddDays(-1), today.AddDays(10), true);
        var expiredPromo = Promotion.Create(
            "CU2025", "Ưu đãi đã hết hạn năm ngoái", "Không còn áp dụng",
            "Percentage", 20, [],
            today.AddDays(-30), today.AddDays(-10), true);
        _db.Promotions.AddRange(activePromo, expiredPromo);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(_userId, _conversation.Id, "Có ưu đãi gì không?");

        result.Reply.Should().Be("Câu trả lời test");
        result.SuggestBooking.Should().BeFalse();

        await _aiChatService.Received(1).AskAsync(
            Arg.Is<string>(s => s.Contains("Giảm 10% dịp hè") && !s.Contains("Ưu đãi đã hết hạn năm ngoái")),
            "Có ưu đãi gì không?",
            Arg.Any<CancellationToken>());

        var messages = await _db.ChatMessages
            .Where(m => m.ConversationId == _conversation.Id)
            .ToListAsync();

        messages.Should().HaveCount(2);
        messages.Should().Contain(m => m.Role == "user" && m.Content == "Có ưu đãi gì không?");
        messages.Should().Contain(m => m.Role == "assistant" && m.Content == "Câu trả lời test");
    }
}
