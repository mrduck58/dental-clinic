using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;

namespace DentalClinic.API.Application.UseCases.Chat;

public class StartConversationHandler(
    IPatientRepository patientRepository,
    IUserRepository userRepository,
    AppDbContext dbContext)
{
    public async Task<StartConversationResult> HandleAsync(Guid userId, CancellationToken ct = default)
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
        await dbContext.SaveChangesAsync(ct);

        return new StartConversationResult(conversation.Id);
    }
}
