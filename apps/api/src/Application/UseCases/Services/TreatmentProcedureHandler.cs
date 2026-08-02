using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Services;

public record ProcedureStepRequest(int StepNumber, string Name);

public class TreatmentProcedureDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
}

public record GetTreatmentProceduresQuery(Guid ServiceId) : IRequest<List<TreatmentProcedureDto>>;

/// <summary>Thay toàn bộ quy trình điều trị của một dịch vụ (xóa hết bước cũ, thêm bước mới).</summary>
public record ReplaceTreatmentProceduresCommand(Guid ServiceId, List<ProcedureStepRequest> Steps) : IRequest<List<TreatmentProcedureDto>>;

public class TreatmentProcedureHandler(
    ITreatmentProcedureRepository treatmentProcedureRepository,
    IServiceRepository serviceRepository) :
    IRequestHandler<GetTreatmentProceduresQuery, List<TreatmentProcedureDto>>,
    IRequestHandler<ReplaceTreatmentProceduresCommand, List<TreatmentProcedureDto>>
{
    public async Task<List<TreatmentProcedureDto>> Handle(GetTreatmentProceduresQuery request, CancellationToken cancellationToken)
    {
        return await GetByServiceAsync(request.ServiceId, cancellationToken);
    }

    public async Task<List<TreatmentProcedureDto>> Handle(ReplaceTreatmentProceduresCommand request, CancellationToken cancellationToken)
    {
        var serviceId = request.ServiceId;
        var steps = request.Steps;

        var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service is null)
            throw new NotFoundException("Không tìm thấy dịch vụ.");

        if (steps.Any(s => string.IsNullOrWhiteSpace(s.Name)))
            throw new ValidationException("Tên bước điều trị không được để trống.");

        if (steps.Select(s => s.StepNumber).Distinct().Count() != steps.Count)
            throw new ValidationException("Số thứ tự các bước không được trùng nhau.");

        var newProcedures = steps
            .OrderBy(s => s.StepNumber)
            .Select(step => TreatmentProcedure.Create(serviceId, step.StepNumber, step.Name.Trim()));

        await treatmentProcedureRepository.ReplaceAllForServiceAsync(serviceId, newProcedures, cancellationToken);

        return await GetByServiceAsync(serviceId, cancellationToken);
    }

    private async Task<List<TreatmentProcedureDto>> GetByServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var procedures = await treatmentProcedureRepository.GetByServiceIdAsync(serviceId, cancellationToken);

        return procedures
            .Select(p => new TreatmentProcedureDto
            {
                Id = p.Id,
                ServiceId = p.ServiceId,
                StepNumber = p.StepNumber,
                Name = p.Name
            })
            .ToList();
    }
}
