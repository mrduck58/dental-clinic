using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Chat;

public class StartConversationHandler(
    IPatientRepository patientRepository,
    IUserRepository userRepository,
    AppDbContext dbContext)
{
    /// <summary>Chỉ nhắc lịch hẹn diễn ra trong khoảng thời gian gần — nhắc quá xa (vd. còn 2 tuần)
    /// không hữu ích và dễ gây khó chịu mỗi lần bệnh nhân mở một cuộc trò chuyện mới.</summary>
    private const int ReminderWindowHours = 48;

    public async Task<StartConversationResult> HandleAsync(
        Guid userId, string? language = null, CancellationToken ct = default)
    {
        // Bệnh nhân mới đăng ký, chưa từng đặt lịch, sẽ chưa có hồ sơ Patient (hồ sơ này hiện chỉ được
        // tạo lười biếng khi đặt lịch lần đầu — CreateAppointmentHandler). Chatbot phải dùng được ngay
        // sau khi đăng nhập nên áp dụng đúng cách tạo lười biếng tương tự ở đây.
        var patient = await patientRepository.GetByUserIdAsync(userId, ct);
        if (patient is null)
        {
            var user = await userRepository.GetByIdAsync(userId, ct)
                ?? throw new NotFoundException("Không tìm thấy tài khoản.");

            patient = Patient.Create(
                user.FullName ?? user.Email,
                user.DateOfBirth ?? new DateOnly(1990, 1, 1),
                user.Gender ?? "Nam",
                userId);
            await patientRepository.AddAsync(patient, ct);
        }

        var conversation = ChatConversation.Create(patient.Id);
        dbContext.ChatConversations.Add(conversation);

        // Chủ động nhắc lịch hẹn sắp tới ngay khi bắt đầu một cuộc trò chuyện MỚI, thay vì hoàn toàn
        // bị động chờ được hỏi — chỉ áp dụng cho hội thoại mới (không nhắc lại khi mở lại lịch sử cũ).
        var reminder = await BuildReminderMessageAsync(patient, language, ct);
        if (reminder is not null)
        {
            dbContext.ChatMessages.Add(ChatMessage.Create(conversation.Id, "assistant", reminder));
        }

        await dbContext.SaveChangesAsync(ct);

        return new StartConversationResult(conversation.Id, reminder);
    }

    private async Task<string?> BuildReminderMessageAsync(Patient patient, string? language, CancellationToken ct)
    {
        var familyMembers = await patientRepository.GetFamilyMembersAsync(patient.Id, ct);
        var relevantPatientIds = new List<Guid> { patient.Id };
        relevantPatientIds.AddRange(familyMembers.Select(f => f.Id));

        var now = DateTimeOffset.UtcNow;
        var windowEnd = now.AddHours(ReminderWindowHours);

        var nextAppointment = await dbContext.Appointments
            .Include(a => a.Dentist)
            .Include(a => a.Patient)
            .Where(a => relevantPatientIds.Contains(a.PatientId) &&
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) &&
                a.AppointmentDate >= now && a.AppointmentDate <= windowEnd)
            .OrderBy(a => a.AppointmentDate)
            .FirstOrDefaultAsync(ct);

        if (nextAppointment is null)
        {
            return null;
        }

        var localTime = nextAppointment.AppointmentDate.UtcDateTime.AddHours(7);
        var isEnglish = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);
        var who = nextAppointment.PatientId == patient.Id
            ? (isEnglish ? "you" : "bạn")
            : nextAppointment.Patient.FullName;

        return isEnglish
            ? $"👋 Reminder: {who} have an upcoming appointment with {nextAppointment.Dentist.FullName} " +
              $"at {localTime:HH:mm} on {localTime:dd/MM/yyyy}. Let me know if you need anything!"
            : $"👋 Nhắc bạn: {who} có lịch hẹn với {nextAppointment.Dentist.FullName} lúc " +
              $"{localTime:HH:mm} ngày {localTime:dd/MM/yyyy}. Bạn cần tôi hỗ trợ gì thêm không?";
    }
}
