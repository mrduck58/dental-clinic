using DentalClinic.API.Application.UseCases.Chat;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetConversationMessagesHandlerTests
{
    private AppDbContext _db = null!;
    private IPatientRepository _patientRepo = null!;
    private GetConversationMessagesHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _patientRepo = Substitute.For<IPatientRepository>();
        _handler = new GetConversationMessagesHandler(_patientRepo, _db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Không tìm thấy hồ sơ bệnh nhân của user phải ném NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_NoPatientProfile_ThrowsNotFoundException()
    {
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Patient?)null);

        Func<Task> act = () => _handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Cuộc trò chuyện không tồn tại phải ném NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_ConversationNotFound_ThrowsNotFoundException()
    {
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(patient);

        Func<Task> act = () => _handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Cuộc trò chuyện thuộc về bệnh nhân khác phải trả về NotFoundException chung
    /// (không tiết lộ sự tồn tại của cuộc trò chuyện người khác).</summary>
    [Test]
    public async Task HandleAsync_ConversationBelongsToAnotherPatient_ThrowsNotFoundException()
    {
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        var otherPatient = Patient.Create(Guid.Empty, new DateOnly(1991, 1, 1), "Nữ");
        _db.Patients.AddRange(patient, otherPatient);
        var conversation = ChatConversation.Create(otherPatient.Id);
        _db.ChatConversations.Add(conversation);
        await _db.SaveChangesAsync();
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(patient);

        Func<Task> act = () => _handler.HandleAsync(Guid.NewGuid(), conversation.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Tin nhắn trong cuộc trò chuyện phải được trả về theo đúng thứ tự thời gian tạo.</summary>
    [Test]
    public async Task HandleAsync_ValidConversation_ReturnsMessagesOrderedByCreatedAt()
    {
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Patients.Add(patient);
        var conversation = ChatConversation.Create(patient.Id);
        _db.ChatConversations.Add(conversation);
        await _db.SaveChangesAsync();

        var first = ChatMessage.Create(conversation.Id, "user", "Xin chào");
        var second = ChatMessage.Create(conversation.Id, "assistant", "Chào bạn, tôi có thể giúp gì?");
        _db.ChatMessages.AddRange(second, first); // cố ý thêm ngược thứ tự để kiểm tra sắp xếp
        await _db.SaveChangesAsync();
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _handler.HandleAsync(Guid.NewGuid(), conversation.Id);

        result.Messages.Should().HaveCount(2);
        result.Messages[0].Content.Should().Be("Xin chào");
        result.Messages[1].Content.Should().Be("Chào bạn, tôi có thể giúp gì?");
    }
}
