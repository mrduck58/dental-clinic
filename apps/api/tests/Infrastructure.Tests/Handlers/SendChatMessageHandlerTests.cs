using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Application.UseCases.Chat;
using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class SendChatMessageHandlerTests
{
    private AppDbContext _db = null!;
    private IPatientRepository _patientRepo = null!;
    private IClinicInfoRepository _clinicInfoRepo = null!;
    private IUserRepository _userRepo = null!;
    private IAppointmentRepository _appointmentRepo = null!;
    private IAiChatService _aiChatService = null!;
    private SendChatMessageHandler _handler = null!;
    private Guid _userId;
    private Patient _patient = null!;
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

        _patient = Patient.Create("Bệnh nhân Test", new DateOnly(1990, 1, 1), "Nam", user.Id);
        _db.Patients.Add(_patient);

        _conversation = ChatConversation.Create(_patient.Id);
        _db.ChatConversations.Add(_conversation);

        await _db.SaveChangesAsync();

        _patientRepo = Substitute.For<IPatientRepository>();
        _patientRepo.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(_patient);

        _clinicInfoRepo = Substitute.For<IClinicInfoRepository>();
        _clinicInfoRepo.GetAsync(Arg.Any<CancellationToken>()).Returns((DentalClinic.API.Domain.Entities.ClinicInfo?)null);

        _userRepo = Substitute.For<IUserRepository>();
        _userRepo.GetStaffPagedAsync(null, null, null, 1, 500, Arg.Any<CancellationToken>())
            .Returns((new List<User>(), 0));

        _aiChatService = Substitute.For<IAiChatService>();
        _aiChatService.AskAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiChatReply("Câu trả lời test", false));

        _patientRepo.GetFamilyMembersAsync(_patient.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Patient>());

        _appointmentRepo = Substitute.For<IAppointmentRepository>();
        _appointmentRepo.GetByDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Appointment>());
        // UpdateAppointmentStatusHandler.CancelAsync đọc/ghi qua IAppointmentRepository trực tiếp
        // (không qua AppDbContext) — proxy 2 method này sang cùng AppDbContext để hủy lịch thật sự
        // cập nhật Status trong DB mà test dùng để kiểm tra kết quả.
        _appointmentRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => _db.Appointments.FirstOrDefault(a => a.Id == ci.Arg<Guid>()));
        _appointmentRepo.UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _appointmentRepo.GetDentistUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.IsAuthenticated.Returns(true);
        currentUserService.UserRole.Returns("Patient");
        currentUserService.UserId.Returns(_userId);

        var getDentistsHandler = new GetDentistsHandler(_userRepo);
        var getDentistSlotsHandler = new GetDentistSlotsHandler(_db, _appointmentRepo);
        var createAppointmentHandler = new CreateAppointmentHandler(
            _appointmentRepo, _patientRepo, _userRepo,
            Substitute.For<IServiceRepository>(), Substitute.For<INotificationService>());
        var updateAppointmentStatusHandler = new UpdateAppointmentStatusHandler(
            _appointmentRepo, Substitute.For<IActivityLogService>(), Substitute.For<INotificationService>(),
            currentUserService, _patientRepo);
        _handler = new SendChatMessageHandler(
            _patientRepo, _clinicInfoRepo, getDentistsHandler, getDentistSlotsHandler,
            createAppointmentHandler, updateAppointmentStatusHandler, _aiChatService, _db,
            NullLogger<SendChatMessageHandler>.Instance);
    }

    /// <summary>
    /// Tạo bác sĩ (kèm user) và ca làm việc 08:00-10:00 cho một ngày — dữ liệu tối thiểu để
    /// GetDentistSlotsHandler sinh được khung giờ trống cho bác sĩ đó trong ngày.
    /// </summary>
    private async Task<Dentist> SeedDentistWithScheduleAsync(DateOnly date)
    {
        var dentistUser = User.Create(
            $"dentist-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", "Dentist",
            fullName: "BS Nguyễn Văn A");
        _db.Users.Add(dentistUser);

        var dentist = Dentist.Create(dentistUser.Id, "BS Nguyễn Văn A", "Chỉnh nha", 5);
        _db.Dentists.Add(dentist);

        _db.WorkSchedules.Add(WorkSchedule.Create(
            date, "08:00-10:00", "dentist", "Dentist",
            staffName: dentist.FullName, room: "P1", roomColor: "#fff", isHoliday: false));

        await _db.SaveChangesAsync();
        return dentist;
    }

    /// <summary>Ngày mai theo giờ Việt Nam — mọi khung giờ trong ngày đều chưa trôi qua.</summary>
    private static DateOnly TomorrowVn()
        => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)).AddDays(1);

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

    /// <summary>
    /// AI lỗi (timeout, 429, chưa cấu hình...) không được làm mất tin nhắn của bệnh nhân hay
    /// đổ 500 ra client — tin nhắn user vẫn phải được lưu và bot trả câu fallback thân thiện.
    /// </summary>
    [Test]
    public async Task HandleAsync_AiFails_PersistsUserMessageAndReturnsFallback()
    {
        _aiChatService.AskAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Gemini 503"));

        var result = await _handler.HandleAsync(_userId, _conversation.Id, "Phòng khám mở cửa mấy giờ?");

        result.SuggestBooking.Should().BeFalse();
        result.Reply.Should().Contain("Xin lỗi");

        var messages = await _db.ChatMessages
            .Where(m => m.ConversationId == _conversation.Id)
            .ToListAsync();
        messages.Should().Contain(m => m.Role == "user" && m.Content == "Phòng khám mở cửa mấy giờ?");
        messages.Should().Contain(m => m.Role == "assistant");
    }

    [Test]
    public async Task HandleAsync_EmptyOrTooLongMessage_ThrowsValidationWithoutCallingAi()
    {
        var actEmpty = () => _handler.HandleAsync(_userId, _conversation.Id, "   ");
        await actEmpty.Should().ThrowAsync<ValidationException>();

        var actTooLong = () => _handler.HandleAsync(_userId, _conversation.Id, new string('a', 2001));
        await actTooLong.Should().ThrowAsync<ValidationException>();

        await _aiChatService.DidNotReceive().AskAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_TooManyMessagesWithinOneMinute_ThrowsValidation()
    {
        for (var i = 0; i < 10; i++)
        {
            _db.ChatMessages.Add(ChatMessage.Create(_conversation.Id, "user", $"tin nhắn {i}"));
        }
        await _db.SaveChangesAsync();

        var act = () => _handler.HandleAsync(_userId, _conversation.Id, "tin nhắn thứ 11");

        await act.Should().ThrowAsync<ValidationException>();
        await _aiChatService.DidNotReceive().AskAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_EnglishLanguage_InstructsBotToReplyInEnglish()
    {
        await _handler.HandleAsync(_userId, _conversation.Id, "What are your opening hours?", language: "en");

        await _aiChatService.Received(1).AskAsync(
            Arg.Is<string>(s => s.Contains("tiếng Anh")),
            "What are your opening hours?",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Bác sĩ có ca làm việc ngày mai phải xuất hiện trong mục "Lịch trống" của system instruction
    /// kèm khung giờ trống — nếu không bot không thể trả lời "còn giờ nào trống" hay đặt lịch được.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistHasScheduleTomorrow_AvailabilityAppearsInSystemInstruction()
    {
        await SeedDentistWithScheduleAsync(TomorrowVn());

        await _handler.HandleAsync(_userId, _conversation.Id, "Ngày mai còn giờ nào trống?");

        await _aiChatService.Received(1).AskAsync(
            Arg.Is<string>(s =>
                s.Contains("Lịch trống") &&
                s.Contains("BS Nguyễn Văn A: 08:00, 08:30, 09:00, 09:30")),
            "Ngày mai còn giờ nào trống?",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Bệnh nhân đã xác nhận đặt lịch (confirmBooking = true, đủ bác sĩ + ngày + giờ trống) →
    /// lịch hẹn phải được tạo thật qua CreateAppointmentHandler, kết quả trả BookingCreated = true
    /// kèm mã lịch hẹn để mobile hiển thị nút "Xem lịch hẹn".
    /// </summary>
    [Test]
    public async Task HandleAsync_ConfirmedBookingWithFreeSlot_CreatesAppointmentAndReturnsCode()
    {
        var tomorrow = TomorrowVn();
        var dentist = await SeedDentistWithScheduleAsync(tomorrow);

        _aiChatService.AskAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiChatReply(
                "Tôi sẽ đặt lịch ngay.", false,
                DentistNameHint: "BS Nguyễn Văn A",
                PreferredDate: tomorrow,
                ConfirmBooking: true,
                PreferredTime: new TimeOnly(8, 0)));

        var result = await _handler.HandleAsync(_userId, _conversation.Id, "Đồng ý, đặt giúp tôi");

        result.BookingCreated.Should().BeTrue();
        result.AppointmentCode.Should().NotBeNullOrEmpty();
        result.SuggestBooking.Should().BeFalse();
        result.Reply.Should().Contain("Đặt lịch thành công");

        await _appointmentRepo.Received(1).AddAsync(
            Arg.Is<Appointment>(a => a.DentistId == dentist.Id),
            Arg.Any<CancellationToken>());

        // Tin nhắn assistant được lưu phải là câu xác nhận thật (kết quả đặt lịch), không phải câu của AI.
        var assistantMessage = await _db.ChatMessages
            .Where(m => m.ConversationId == _conversation.Id && m.Role == "assistant")
            .SingleAsync();
        assistantMessage.Content.Should().Contain("Đặt lịch thành công");
    }

    /// <summary>
    /// Khung giờ bệnh nhân xác nhận đã bị người khác chiếm → KHÔNG được tạo lịch hẹn,
    /// bot phải trả lời khung giờ không còn trống để bệnh nhân chọn lại.
    /// </summary>
    [Test]
    public async Task HandleAsync_ConfirmedBookingButSlotTaken_DoesNotCreateAppointment()
    {
        var tomorrow = TomorrowVn();
        var dentist = await SeedDentistWithScheduleAsync(tomorrow);

        // 08:00 ngày mai (giờ VN) đã có người đặt trước
        var occupied = Appointment.Create(
            Guid.NewGuid(), dentist.Id,
            new DateTimeOffset(tomorrow.ToDateTime(new TimeOnly(8, 0)), TimeSpan.FromHours(7)));
        _appointmentRepo.GetByDateAsync(tomorrow, Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { occupied });

        _aiChatService.AskAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiChatReply(
                "Tôi sẽ đặt lịch ngay.", false,
                DentistNameHint: "BS Nguyễn Văn A",
                PreferredDate: tomorrow,
                ConfirmBooking: true,
                PreferredTime: new TimeOnly(8, 0)));

        var result = await _handler.HandleAsync(_userId, _conversation.Id, "Đồng ý, đặt giúp tôi");

        result.BookingCreated.Should().BeFalse();
        result.AppointmentCode.Should().BeNull();
        result.Reply.Should().Contain("không còn đặt được");

        await _appointmentRepo.DidNotReceive().AddAsync(
            Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// AI báo xác nhận nhưng thiếu thông tin (không đối chiếu được bác sĩ) → không tạo lịch,
    /// trả lời thân thiện và bật suggestBooking để bệnh nhân có lối thoát qua màn hình Đặt lịch.
    /// </summary>
    [Test]
    public async Task HandleAsync_ConfirmedBookingMissingDentist_ReturnsFriendlyFailure()
    {
        _aiChatService.AskAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiChatReply(
                "Tôi sẽ đặt lịch ngay.", false,
                DentistNameHint: "BS Không Tồn Tại",
                PreferredDate: TomorrowVn(),
                ConfirmBooking: true,
                PreferredTime: new TimeOnly(8, 0)));

        var result = await _handler.HandleAsync(_userId, _conversation.Id, "Đồng ý");

        result.BookingCreated.Should().BeFalse();
        result.SuggestBooking.Should().BeTrue();
        result.Reply.Should().Contain("chưa thể đặt lịch");

        await _appointmentRepo.DidNotReceive().AddAsync(
            Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi bookingHint.patientName khớp với một người thân đã đăng ký, lịch hẹn phải được tạo với
    /// PatientId của NGƯỜI THÂN đó, không phải của chính chủ tài khoản đang chat.
    /// </summary>
    [Test]
    public async Task HandleAsync_ConfirmedBookingForFamilyMember_CreatesAppointmentForFamilyMember()
    {
        var tomorrow = TomorrowVn();
        var dentist = await SeedDentistWithScheduleAsync(tomorrow);

        var child = Patient.Create(
            "Bé Bún", new DateOnly(2018, 1, 1), "Nam", primaryPatientId: _patient.Id, relationship: "Con");
        _db.Patients.Add(child);
        await _db.SaveChangesAsync();
        _patientRepo.GetFamilyMembersAsync(_patient.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Patient> { child });
        _patientRepo.GetByIdAsync(child.Id, Arg.Any<CancellationToken>()).Returns(child);

        _aiChatService.AskAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiChatReply(
                "Tôi sẽ đặt lịch cho bé.", false,
                DentistNameHint: "BS Nguyễn Văn A",
                PreferredDate: tomorrow,
                ConfirmBooking: true,
                PreferredTime: new TimeOnly(8, 0),
                PatientNameHint: "Bé Bún"));

        var result = await _handler.HandleAsync(_userId, _conversation.Id, "Đồng ý, đặt cho bé Bún giúp tôi");

        result.BookingCreated.Should().BeTrue();
        result.Reply.Should().Contain("Bé Bún");

        await _appointmentRepo.Received(1).AddAsync(
            Arg.Is<Appointment>(a => a.DentistId == dentist.Id && a.PatientId == child.Id),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Xác nhận hủy đúng lịch hẹn sắp tới (khớp mã) phải chuyển Status sang Cancelled thật.</summary>
    [Test]
    public async Task HandleAsync_ConfirmedCancelWithMatchingCode_CancelsAppointment()
    {
        var tomorrow = TomorrowVn();
        var dentist = await SeedDentistWithScheduleAsync(tomorrow);
        var appointment = Appointment.Create(
            _patient.Id, dentist.Id, new DateTimeOffset(tomorrow.ToDateTime(new TimeOnly(8, 0)), TimeSpan.FromHours(7)));
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        var code = $"DK{appointment.AppointmentDate:yyyyMMdd}{appointment.Id.ToString("N")[..6].ToUpper()}";

        _aiChatService.AskAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiChatReply(
                "Tôi sẽ hủy lịch này.", false,
                ConfirmCancel: true,
                CancelAppointmentCodeHint: code));

        var result = await _handler.HandleAsync(_userId, _conversation.Id, "Đồng ý, hủy giúp tôi");

        result.BookingCancelled.Should().BeTrue();
        result.AppointmentCode.Should().Be(code);
        result.Reply.Should().Contain("Đã hủy");

        var reloaded = _db.Appointments.Single(a => a.Id == appointment.Id);
        reloaded.Status.Should().Be(DentalClinic.API.Domain.Enums.AppointmentStatus.Cancelled);
    }

    /// <summary>Mã lịch hẹn AI đọc không khớp lịch sắp tới nào (bịa hoặc sai) → KHÔNG được hủy bất kỳ
    /// lịch nào, phải trả lời không tìm thấy.</summary>
    [Test]
    public async Task HandleAsync_ConfirmedCancelWithUnknownCode_DoesNotCancelAnything()
    {
        var tomorrow = TomorrowVn();
        var dentist = await SeedDentistWithScheduleAsync(tomorrow);
        var appointment = Appointment.Create(
            _patient.Id, dentist.Id, new DateTimeOffset(tomorrow.ToDateTime(new TimeOnly(8, 0)), TimeSpan.FromHours(7)));
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        _aiChatService.AskAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiChatReply(
                "Tôi sẽ hủy lịch này.", false,
                ConfirmCancel: true,
                CancelAppointmentCodeHint: "DK00000000FFFFFF"));

        var result = await _handler.HandleAsync(_userId, _conversation.Id, "Đồng ý, hủy giúp tôi");

        result.BookingCancelled.Should().BeFalse();

        var reloaded = _db.Appointments.Single(a => a.Id == appointment.Id);
        reloaded.Status.Should().Be(DentalClinic.API.Domain.Enums.AppointmentStatus.Pending);
    }

    /// <summary>Xác nhận dời đúng lịch hẹn sang ngày/giờ mới còn trống phải: tạo lịch hẹn MỚI (cùng
    /// bác sĩ), rồi hủy lịch hẹn GỐC — cả hai thao tác đều phải xảy ra thật trong DB.</summary>
    [Test]
    public async Task HandleAsync_ConfirmedRescheduleWithFreeSlot_CreatesNewAppointmentAndCancelsOld()
    {
        var tomorrow = TomorrowVn();
        var dayAfter = tomorrow.AddDays(1);
        var dentist = await SeedDentistWithScheduleAsync(tomorrow);
        // Lịch hẹn gốc chỉ có ca làm việc ngày mai — cần thêm ca cho ngày kia để dời lịch sang đó.
        _db.WorkSchedules.Add(WorkSchedule.Create(
            dayAfter, "08:00-10:00", "dentist", "Dentist",
            staffName: dentist.FullName, room: "P1", roomColor: "#fff", isHoliday: false));
        await _db.SaveChangesAsync();

        var original = Appointment.Create(
            _patient.Id, dentist.Id, new DateTimeOffset(tomorrow.ToDateTime(new TimeOnly(8, 0)), TimeSpan.FromHours(7)));
        _db.Appointments.Add(original);
        await _db.SaveChangesAsync();
        var originalCode = $"DK{original.AppointmentDate:yyyyMMdd}{original.Id.ToString("N")[..6].ToUpper()}";

        _aiChatService.AskAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiChatReply(
                "Tôi sẽ dời lịch này.", false,
                PreferredDate: dayAfter,
                PreferredTime: new TimeOnly(8, 0),
                ConfirmReschedule: true,
                RescheduleAppointmentCodeHint: originalCode));

        var result = await _handler.HandleAsync(_userId, _conversation.Id, "Đồng ý, dời sang ngày kia giúp tôi");

        result.BookingRescheduled.Should().BeTrue();
        result.AppointmentCode.Should().NotBeNullOrEmpty();
        result.AppointmentCode.Should().NotBe(originalCode);
        result.Reply.Should().Contain("dời");

        var reloadedOriginal = _db.Appointments.Single(a => a.Id == original.Id);
        reloadedOriginal.Status.Should().Be(DentalClinic.API.Domain.Enums.AppointmentStatus.Cancelled);

        await _appointmentRepo.Received(1).AddAsync(
            Arg.Is<Appointment>(a => a.DentistId == dentist.Id && a.PatientId == _patient.Id &&
                a.AppointmentDate.Date == dayAfter.ToDateTime(TimeOnly.MinValue).Date),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Mã lịch hẹn AI đọc không khớp lịch sắp tới nào → KHÔNG được tạo lịch mới hay hủy lịch
    /// gốc, phải trả lời không tìm thấy.</summary>
    [Test]
    public async Task HandleAsync_ConfirmedRescheduleWithUnknownCode_DoesNotChangeAnything()
    {
        var tomorrow = TomorrowVn();
        var dentist = await SeedDentistWithScheduleAsync(tomorrow);
        var original = Appointment.Create(
            _patient.Id, dentist.Id, new DateTimeOffset(tomorrow.ToDateTime(new TimeOnly(8, 0)), TimeSpan.FromHours(7)));
        _db.Appointments.Add(original);
        await _db.SaveChangesAsync();

        _aiChatService.AskAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AiChatReply(
                "Tôi sẽ dời lịch này.", false,
                PreferredDate: tomorrow.AddDays(1),
                PreferredTime: new TimeOnly(8, 0),
                ConfirmReschedule: true,
                RescheduleAppointmentCodeHint: "DK00000000FFFFFF"));

        var result = await _handler.HandleAsync(_userId, _conversation.Id, "Đồng ý, dời giúp tôi");

        result.BookingRescheduled.Should().BeFalse();

        var reloadedOriginal = _db.Appointments.Single(a => a.Id == original.Id);
        reloadedOriginal.Status.Should().Be(DentalClinic.API.Domain.Enums.AppointmentStatus.Pending);

        await _appointmentRepo.DidNotReceive().AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }
}
