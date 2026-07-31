using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
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

public record CreateMaterialRequestRequest(Guid AppointmentId, string Content);

/// <summary>Bác sĩ gửi yêu cầu vật tư từ buổi khám → sang trang nhập–xuất vật tư của staff.</summary>
public class CreateMaterialRequestHandler(AppDbContext dbContext)
{
    public async Task<MaterialRequestDto> HandleAsync(CreateMaterialRequestRequest request, CancellationToken ct = default)
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

        dbContext.MaterialRequests.Add(mr);
        await dbContext.SaveChangesAsync(ct);

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

/// <summary>Danh sách yêu cầu vật tư từ bác sĩ (cho trang nhập–xuất vật tư của staff).</summary>
public class GetMaterialRequestsHandler(AppDbContext dbContext)
{
    public async Task<List<MaterialRequestDto>> HandleAsync(string? status, Guid? patientId = null, string? patientName = null, CancellationToken ct = default)
    {
        var query = dbContext.MaterialRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MaterialRequestStatus>(status, true, out var st))
            query = query.Where(m => m.Status == st);

        // Lọc theo bệnh nhân: khớp PatientId (lưu ở CourseId) HOẶC tên (bao gồm dữ liệu cũ chưa có id).
        var hasId = patientId is Guid;
        var hasName = !string.IsNullOrWhiteSpace(patientName);
        if (hasId && hasName)
            query = query.Where(m => m.CourseId == patientId || m.PatientName == patientName);
        else if (hasId)
            query = query.Where(m => m.CourseId == patientId);
        else if (hasName)
            query = query.Where(m => m.PatientName == patientName);

        var rows = await query
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

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

/// <summary>Đánh dấu một yêu cầu vật tư đã được kho xử lý.</summary>
public class MarkMaterialRequestDoneHandler(AppDbContext dbContext)
{
    public async Task HandleAsync(Guid id, string handledBy, CancellationToken ct = default)
    {
        var request = await dbContext.MaterialRequests.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy yêu cầu vật tư.");

        request.MarkDone(handledBy);
        await dbContext.SaveChangesAsync(ct);
    }
}
