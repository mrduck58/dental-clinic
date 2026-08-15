using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.DentistDashboard;

public record DentistPatientDto(
    Guid AppointmentId,
    string AppointmentCode,
    string PatientName,
    int Age,
    string Gender,
    string? Phone,
    DateTimeOffset AppointmentDate,
    string Status,
    string? ServiceName,
    string? Symptoms,
    bool IsNew,
    bool IsFollowUpVisit, // Buổi hẹn do staff check-in từ tab Tái khám (gắn về buổi gốc)
    int QueueNumber = 0,
    DateTimeOffset? CheckedInAt = null,
    int WaitMinutes = 0);

public record DentistPatientsResponse(
    DateOnly Date,
    int TotalWaiting,
    int TotalInProgress,
    int TotalCompleted,
    List<DentistPatientDto> Patients);

public record GetDentistPatientsQuery(Guid DentistId, DateOnly Date) : IRequest<DentistPatientsResponse>;

public class GetDentistPatientsHandler(IDentistDashboardQueryService dentistDashboardQueryService)
    : IRequestHandler<GetDentistPatientsQuery, DentistPatientsResponse>
{
    public Task<DentistPatientsResponse> Handle(GetDentistPatientsQuery request, CancellationToken ct) =>
        dentistDashboardQueryService.GetPatientsAsync(request.DentistId, request.Date, ct);
}

public static class DentistPatientMapper
{
    public static int CalculateAge(DateOnly? dateOfBirth)
    {
        if (!dateOfBirth.HasValue) return 0;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value > today.AddYears(-age)) age--;
        return age;
    }
}
