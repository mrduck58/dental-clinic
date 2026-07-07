using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record CreateWalkInCommand(
    Guid DentistId,
    DateTimeOffset AppointmentDate,
    string PatientName,
    string PatientPhone,
    DateOnly DateOfBirth,
    string Gender,
    Guid? ServiceId,
    string? Symptoms);

public record CreateWalkInResult(
    Guid AppointmentId,
    string AppointmentCode,
    string PatientName,
    string Status);

public class CreateWalkInAppointmentHandler(AppDbContext dbContext)
{
    public async Task<CreateWalkInResult> HandleAsync(CreateWalkInCommand cmd, CancellationToken ct = default)
    {
        // 1. Không cho đặt lịch cho khung giờ đã qua (chặn cả trường hợp bypass UI).
        if (cmd.AppointmentDate < DateTimeOffset.UtcNow)
            throw new ValidationException("Không thể đặt lịch cho khung giờ đã qua.");

        // 2. Kiểm tra slot còn trống
        var isBooked = await dbContext.Appointments.AnyAsync(a =>
            a.DentistId == cmd.DentistId &&
            a.AppointmentDate == cmd.AppointmentDate &&
            a.Status != AppointmentStatus.Cancelled, ct);

        if (isBooked)
            throw new ConflictException("Khung giờ này đã được đặt. Vui lòng chọn giờ khác.");

        // 3. Tìm bệnh nhân theo số điện thoại (qua tài khoản), hoặc tạo mới
        var patient = await dbContext.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.User != null && p.User.PhoneNumber == cmd.PatientPhone, ct);

        if (patient == null)
        {
            patient = Patient.Create(cmd.PatientName, cmd.DateOfBirth, cmd.Gender, phoneNumber: cmd.PatientPhone);
            dbContext.Patients.Add(patient);
        }
        else
        {
            // Cập nhật thông tin bệnh nhân tìm được bằng thông tin staff nhập tại quầy
            patient.SetPhoneNumber(cmd.PatientPhone);
            patient.SetDateOfBirth(cmd.DateOfBirth);
            patient.SetGender(cmd.Gender);
        }

        // 4. Tạo lịch hẹn, bỏ qua Pending → Confirmed ngay (đặt tại quầy)
        var appointment = Appointment.Create(
            patient.Id, cmd.DentistId, cmd.AppointmentDate,
            symptoms: cmd.Symptoms, serviceId: cmd.ServiceId);
        appointment.Confirm();

        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(ct);

        var code = $"DK{cmd.AppointmentDate:yyyyMMdd}{appointment.Id.ToString("N")[..6].ToUpper()}";

        return new CreateWalkInResult(appointment.Id, code, patient.FullName, appointment.Status.ToString());
    }
}
