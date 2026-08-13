using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.DentistDashboard;

public record DentistShiftDto(string Label, string Period, string? Room);
public record DentistWeekShiftsDto(int Total, int Morning, int Afternoon, int Evening);

public record DentistDashboardPatientDto(
    Guid AppointmentId,
    string PatientName,
    string? ServiceName,
    string Time,
    string Status);

public record DentistDashboardResponse(
    DateOnly Date,
    int TotalPatientsToday,
    int TotalWaiting,
    int TotalInProgress,
    int TotalCompleted,
    DentistWeekShiftsDto WeekShifts,
    List<DentistShiftDto> TodayShifts,
    List<DentistDashboardPatientDto> UpcomingPatients);

public record GetDentistDashboardQuery(Guid UserId) : IRequest<DentistDashboardResponse>;

public class DentistDashboardHandler(IDentistDashboardQueryService dentistDashboardQueryService)
    : IRequestHandler<GetDentistDashboardQuery, DentistDashboardResponse>
{
    public Task<DentistDashboardResponse> Handle(GetDentistDashboardQuery request, CancellationToken ct) =>
        dentistDashboardQueryService.GetDashboardAsync(request.UserId, ct);
}
