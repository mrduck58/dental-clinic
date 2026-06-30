using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record CreateTreatmentCourseRequest(Guid AppointmentId, string Name, decimal TotalCost, string? Materials = null);
public record UpdateTreatmentCourseRequest(Guid CourseId, string Name, decimal TotalCost);

public class TreatmentCourseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class TreatmentCourseHandler(AppDbContext dbContext)
{
    /// <summary>Bác sĩ tạo liệu trình dài hạn ngay tại buổi khám và gắn buổi hẹn vào liệu trình.</summary>
    public async Task<TreatmentCourseDto> CreateAsync(CreateTreatmentCourseRequest request, CancellationToken ct = default)
    {
        var appointment = await dbContext.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Dentist)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status != AppointmentStatus.InProgress)
            throw new ValidationException("Chỉ có thể tạo liệu trình khi đang khám.");

        if (appointment.CourseId != null)
            throw new ConflictException("Buổi hẹn này đã thuộc một liệu trình.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Tên liệu trình không được để trống.");

        if (request.TotalCost <= 0)
            throw new ValidationException("Tổng chi phí liệu trình phải lớn hơn 0.");

        var course = TreatmentCourse.Create(appointment.PatientId, appointment.DentistId, request.Name.Trim(), request.TotalCost);
        dbContext.TreatmentCourses.Add(course);

        appointment.AssignToCourse(course.Id);

        // Vật liệu cần thiết → gửi sang trang nhập–xuất vật tư của staff.
        if (!string.IsNullOrWhiteSpace(request.Materials))
        {
            var materialRequest = MaterialRequest.Create(
                course.Id,
                course.Name,
                appointment.Patient.FullName,
                appointment.Dentist.FullName,
                request.Materials.Trim());
            dbContext.MaterialRequests.Add(materialRequest);
        }

        await dbContext.SaveChangesAsync(ct);

        return ToDto(course, amountPaid: 0);
    }

    public async Task<TreatmentCourseDto> UpdateAsync(UpdateTreatmentCourseRequest request, CancellationToken ct = default)
    {
        var course = await dbContext.TreatmentCourses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình.");

        if (request.TotalCost <= 0)
            throw new ValidationException("Tổng chi phí liệu trình phải lớn hơn 0.");

        var amountPaid = await GetAmountPaidAsync(course.Id, ct);
        if (request.TotalCost < amountPaid)
            throw new ValidationException("Tổng chi phí mới không được nhỏ hơn số tiền đã thu.");

        course.Update(request.Name.Trim(), request.TotalCost);
        await dbContext.SaveChangesAsync(ct);

        return ToDto(course, amountPaid);
    }

    /// <summary>Lấy thông tin liệu trình gắn với một buổi hẹn (nếu có).</summary>
    public async Task<TreatmentCourseDto?> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appointment = await dbContext.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

        if (appointment?.CourseId == null)
            return null;

        var course = await dbContext.TreatmentCourses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == appointment.CourseId, ct);

        if (course == null) return null;

        var amountPaid = await GetAmountPaidAsync(course.Id, ct);
        return ToDto(course, amountPaid);
    }

    private async Task<decimal> GetAmountPaidAsync(Guid courseId, CancellationToken ct) =>
        await dbContext.Invoices
            .Where(i => i.CourseId == courseId && i.Status == PaymentStatus.Paid)
            .SumAsync(i => (decimal?)i.TotalAmount, ct) ?? 0m;

    private static TreatmentCourseDto ToDto(TreatmentCourse c, decimal amountPaid) => new()
    {
        Id = c.Id,
        Name = c.Name,
        TotalCost = c.TotalCost,
        Status = c.Status.ToString(),
        AmountPaid = amountPaid,
        RemainingAmount = Math.Max(0, c.TotalCost - amountPaid),
        CreatedAt = c.CreatedAt
    };
}
