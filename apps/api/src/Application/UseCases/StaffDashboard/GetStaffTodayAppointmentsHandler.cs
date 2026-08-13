using DentalClinic.API.Application.DTOs.StaffDashboard;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.StaffDashboard;

public record GetStaffTodayAppointmentsQuery(int Limit) : IRequest<IReadOnlyList<StaffTodayAppointmentDto>>;

/// <summary>Lịch hẹn hôm nay đang ở trạng thái cần staff theo dõi (đã xác nhận / đã check-in / đang khám).</summary>
public class GetStaffTodayAppointmentsHandler(IStaffDashboardQueryService staffDashboardQueryService)
    : IRequestHandler<GetStaffTodayAppointmentsQuery, IReadOnlyList<StaffTodayAppointmentDto>>
{
    public Task<IReadOnlyList<StaffTodayAppointmentDto>> Handle(GetStaffTodayAppointmentsQuery query, CancellationToken ct) =>
        staffDashboardQueryService.GetTodayAppointmentsAsync(query.Limit, ct);
}
