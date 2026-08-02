using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Inventory;

public class MaterialRequestDto
{
    public Guid Id { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DentistName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? HandledAt { get; set; }
    public string? HandledBy { get; set; }
}

public record CreateMaterialRequestRequest(Guid AppointmentId, string Content) : IRequest<MaterialRequestDto>;

/// <summary>Bác sĩ gửi yêu cầu vật tư từ buổi khám → sang trang nhập–xuất vật tư của staff.</summary>
public class CreateMaterialRequestHandler(AppDbContext dbContext, IMaterialRequestRepository materialRequestRepository)
    : IRequestHandler<CreateMaterialRequestRequest, MaterialRequestDto>
{
    public async Task<MaterialRequestDto> Handle(CreateMaterialRequestRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ValidationException("Nội dung yêu cầu vật tư không được để trống.");

        var appt = await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Dentist)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        var mr = MaterialRequest.Create(
            courseName: appt.Service?.Name ?? "Khám tổng quát",
            patientName: appt.Patient.FullName,
            dentistName: appt.Dentist.FullName,
            content: request.Content.Trim(),
            courseId: appt.PatientId); // dùng CourseId (cột cũ, không còn dùng cho course) để lưu PatientId

        await materialRequestRepository.AddAsync(mr, ct);

        return new MaterialRequestDto
        {
            Id = mr.Id,
            CourseName = mr.CourseName,
            PatientName = mr.PatientName,
            DentistName = mr.DentistName,
            Content = mr.Content,
            Status = mr.Status.ToString(),
            CreatedAt = mr.CreatedAt
        };
    }
}

public record GetMaterialRequestsQuery(
    string? Status = null,
    Guid? PatientId = null,
    string? PatientName = null) : IRequest<List<MaterialRequestDto>>;

/// <summary>Danh sách yêu cầu vật tư từ bác sĩ (cho trang nhập–xuất vật tư của staff).</summary>
public class GetMaterialRequestsHandler(IMaterialRequestRepository materialRequestRepository)
    : IRequestHandler<GetMaterialRequestsQuery, List<MaterialRequestDto>>
{
    public async Task<List<MaterialRequestDto>> Handle(GetMaterialRequestsQuery query, CancellationToken ct)
    {
        var rows = await materialRequestRepository.SearchAsync(
            query.Status, query.PatientId, query.PatientName, ct);

        return rows.Select(m => new MaterialRequestDto
        {
            Id = m.Id,
            CourseName = m.CourseName,
            PatientName = m.PatientName,
            DentistName = m.DentistName,
            Content = m.Content,
            Status = m.Status.ToString(),
            CreatedAt = m.CreatedAt,
            HandledAt = m.HandledAt,
            HandledBy = m.HandledBy
        }).ToList();
    }
}

public record MarkMaterialRequestDoneCommand(Guid Id, string HandledBy) : IRequest;

/// <summary>Đánh dấu một yêu cầu vật tư đã được kho xử lý.</summary>
public class MarkMaterialRequestDoneHandler(IMaterialRequestRepository materialRequestRepository) : IRequestHandler<MarkMaterialRequestDoneCommand>
{
    public async Task Handle(MarkMaterialRequestDoneCommand command, CancellationToken ct)
    {
        var request = await materialRequestRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy yêu cầu vật tư.");

        request.MarkDone(command.HandledBy);
        await materialRequestRepository.UpdateAsync(request, ct);
    }
}
