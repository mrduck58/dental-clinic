using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

public record GetExaminationQuery(Guid AppointmentId) : IRequest<ExaminationDto?>;

public class GetExaminationHandler(AppDbContext dbContext) : IRequestHandler<GetExaminationQuery, ExaminationDto?>
{
    public async Task<ExaminationDto?> Handle(GetExaminationQuery request, CancellationToken ct)
    {
        var appointmentId = request.AppointmentId;

        var appointment = await dbContext.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist).ThenInclude(d => d.User)
            .Include(a => a.Service)
            .Include(a => a.Diagnoses)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Service)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Dentist)
            .Include(a => a.Prescriptions).ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

        if (appointment == null)
            return null;

        var dto = ClinicalRecordMappers.ToExaminationDto(appointment);
        dto.RelatedAppointmentIds = await ClinicalRecordMappers.GetFollowUpChainAsync(dbContext, appointment.Id, ct);
        return dto;
    }
}
