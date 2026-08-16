using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Schedules;

namespace DentalClinic.API.Application.UseCases.Booking;

/// <summary>
/// Khẳng định một khung giờ của bác sĩ còn trống, có tính đến thời lượng dịch vụ (không chỉ khớp
/// đúng phút bắt đầu) — để một request gửi thẳng không lách được trạng thái disabled của UI khi
/// khung giờ thực chất đã bị một lịch hẹn dài hơn trước đó chiếm dụng.
///
/// Tách khỏi CreateAppointmentHandler để luồng dời lịch dùng đúng cùng một logic; hai bản sao chép
/// tay của cùng quy tắc này sẽ lệch nhau ngay lần đầu ai đó sửa một bên.
/// </summary>
public class AppointmentSlotGuard(
    IAppointmentRepository appointmentRepository,
    IServiceRepository serviceRepository)
{
    /// <param name="excludeAppointmentId">
    /// Lịch hẹn đang được dời — phải loại khỏi danh sách chiếm chỗ, nếu không nó tự chặn chính mình
    /// khi người dùng chỉ đổi bác sĩ hoặc dịch vụ mà giữ nguyên giờ.
    /// </param>
    public async Task EnsureSlotAvailableAsync(
        Guid dentistId,
        DateTimeOffset appointmentDate,
        Guid? serviceId,
        Guid? excludeAppointmentId,
        CancellationToken ct)
    {
        var localTime = appointmentDate.UtcDateTime.AddHours(7);
        var service = serviceId.HasValue ? await serviceRepository.GetByIdAsync(serviceId.Value, ct) : null;
        var newRange = SlotCalculator.BuildOccupiedRange(localTime.Hour, localTime.Minute, service?.DurationMinutes);

        var dayAppointments = await appointmentRepository.GetByDateAsync(DateOnly.FromDateTime(localTime), ct);

        var existingRanges = dayAppointments
            .Where(a => a.DentistId == dentistId && a.Id != excludeAppointmentId)
            .Select(a =>
            {
                var otherLocal = a.AppointmentDate.UtcDateTime.AddHours(7);
                return SlotCalculator.BuildOccupiedRange(otherLocal.Hour, otherLocal.Minute, a.Service?.DurationMinutes, a.Status);
            });

        if (SlotCalculator.IsOccupied(newRange.StartMinutes, newRange.EndMinutes, existingRanges))
            throw new ConflictException("Khung giờ này đã được đặt. Vui lòng chọn giờ khác.");
    }
}
