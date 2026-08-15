using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

/// <summary>
/// Xem trước ảnh hưởng của một đơn xin nghỉ: những ca làm việc đã xếp bị trùng vào khoảng nghỉ
/// (sẽ bị gỡ nếu duyệt) và những lịch hẹn đã đặt trong các ngày đó. Chỉ Owner gọi — tầng
/// Presentation chặn vai trò, handler không tự đọc claim.
/// </summary>
public record GetLeaveRequestImpactQuery(Guid Id) : IRequest<LeaveImpactDto>;

public class GetLeaveRequestImpactHandler(
    ILeaveRequestRepository leaveRequestRepository,
    IWorkScheduleRepository workScheduleRepository,
    IAppointmentRepository appointmentRepository) : IRequestHandler<GetLeaveRequestImpactQuery, LeaveImpactDto>
{
    public async Task<LeaveImpactDto> Handle(GetLeaveRequestImpactQuery query, CancellationToken ct)
    {
        var request = await leaveRequestRepository.GetByIdAsync(query.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy đơn nghỉ phép với ID: {query.Id}");

        var shifts = await LeaveImpactBuilder.GetAffectedShiftsAsync(request, workScheduleRepository, ct);
        var appointments = await LeaveImpactBuilder.GetAffectedAppointmentsAsync(request, appointmentRepository, ct);

        return LeaveImpactBuilder.Build(request, shifts, appointments);
    }
}
