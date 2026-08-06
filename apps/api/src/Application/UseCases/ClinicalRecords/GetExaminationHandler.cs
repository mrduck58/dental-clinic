using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

public record GetExaminationQuery(Guid AppointmentId) : IRequest<ExaminationDto?>;

public class GetExaminationHandler(IAppointmentRepository appointmentRepository) : IRequestHandler<GetExaminationQuery, ExaminationDto?>
{
    public async Task<ExaminationDto?> Handle(GetExaminationQuery request, CancellationToken ct)
    {
        var appointmentId = request.AppointmentId;

        var appointment = await appointmentRepository.GetExaminationDetailAsync(appointmentId, ct);

        if (appointment == null)
            return null;

        var dto = ClinicalRecordMappers.ToExaminationDto(appointment);
        dto.RelatedAppointmentIds = await appointmentRepository.GetFollowUpChainAsync(appointment.Id, ct);
        return dto;
    }
}
