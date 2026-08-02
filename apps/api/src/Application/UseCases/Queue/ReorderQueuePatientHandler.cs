using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Queue;

public record ReorderQueuePatientCommand(Guid AppointmentId, Guid SwapWithAppointmentId) : IRequest;

/// <summary>
/// Đổi chỗ hai bệnh nhân ĐANG CHỜ cạnh nhau trong hàng đợi — lễ tân bấm nút đẩy lên/xuống một bậc.
/// Chỉ hoán đổi vị trí (<see cref="Appointment.QueueOrder"/>), không đụng giờ check-in nên thời gian
/// chờ của cả hai giữ nguyên. Frontend luôn gửi kèm người liền kề trong CÙNG hàng đợi phòng.
/// </summary>
public class ReorderQueuePatientHandler(AppDbContext dbContext) : IRequestHandler<ReorderQueuePatientCommand>
{
    public async Task Handle(ReorderQueuePatientCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;
        var swapWithAppointmentId = command.SwapWithAppointmentId;

        if (appointmentId == swapWithAppointmentId)
            throw new ValidationException("Không thể đổi chỗ với chính bệnh nhân đó.");

        var a = await dbContext.Appointments.FirstOrDefaultAsync(x => x.Id == appointmentId, ct)
            ?? throw new NotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        var b = await dbContext.Appointments.FirstOrDefaultAsync(x => x.Id == swapWithAppointmentId, ct)
            ?? throw new NotFoundException($"Không tìm thấy lịch hẹn {swapWithAppointmentId}.");

        // Chỉ đổi thứ tự giữa hai người đang chờ — ai đã vào khám/khám xong không còn trong hàng.
        if (a.Status != AppointmentStatus.CheckedIn || b.Status != AppointmentStatus.CheckedIn)
            throw new ConflictException("Chỉ đổi thứ tự được giữa hai bệnh nhân đang chờ khám.");

        // Hoán đổi vị trí hiệu dụng: người chưa từng chỉnh tay thì lấy mốc giờ check-in làm vị trí.
        var ea = EffectiveOrder(a);
        var eb = EffectiveOrder(b);
        a.SetQueueOrder(eb);
        b.SetQueueOrder(ea);

        await dbContext.SaveChangesAsync(ct);
    }

    private static long EffectiveOrder(Appointment a)
        => a.QueueOrder ?? (a.CheckedInAt ?? a.AppointmentDate).UtcTicks;
}
