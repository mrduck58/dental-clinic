using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Booking;

public record AppointmentsPagedDto(
    IReadOnlyList<StaffAppointmentDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record GetAppointmentsPagedQuery(
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Status,
    string? Search,
    int Page = 1,
    int PageSize = 20,
    string? SortDir = null) : IRequest<AppointmentsPagedDto>;

public class GetAppointmentsPagedHandler(IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetAppointmentsPagedQuery, AppointmentsPagedDto>
{
    public async Task<AppointmentsPagedDto> Handle(GetAppointmentsPagedQuery query, CancellationToken ct)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);

        var (items, total) = await appointmentRepository.GetAppointmentsPagedAsync(
            query.StartDate, query.EndDate, query.Status, query.Search, page, pageSize, query.SortDir, ct);

        var dtos = items.Select(a =>
        {
            var accountHolder = a.Patient.PrimaryPatient ?? a.Patient;
            return new StaffAppointmentDto(
                a.Id,
                $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString()[..6].ToUpper()}",
                a.PatientId,
                a.Patient.FullName,
                a.Patient.User?.PhoneNumber,
                a.Dentist.FullName,
                a.Service?.Name,
                a.AppointmentDate,
                a.CreatedAt,
                a.Status.ToString(),
                a.Symptoms,
                a.CheckedInAt,
                a.Origin.ToString(),
                a.Patient.Relationship,
                accountHolder.FullName,
                accountHolder.User?.Email);
        }).ToList();

        return new AppointmentsPagedDto(
            dtos,
            total,
            page,
            pageSize,
            (int)Math.Ceiling((double)total / pageSize));
    }
}
