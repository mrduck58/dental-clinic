using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

/// <param name="PatientId">
/// Hồ sơ bệnh nhân staff đã chọn từ ô tra cứu. Khi có giá trị, dùng thẳng hồ sơ này thay vì
/// dò theo số điện thoại — tránh tạo trùng khi bệnh nhân đổi số hoặc có nhiều người trùng số.
/// </param>
public record CreateWalkInCommand(
    Guid DentistId,
    DateTimeOffset AppointmentDate,
    string PatientName,
    string PatientPhone,
    DateOnly DateOfBirth,
    string Gender,
    Guid? ServiceId,
    string? Symptoms,
    Guid? PatientId = null);

public record CreateWalkInResult(
    Guid AppointmentId,
    string AppointmentCode,
    string PatientName,
    string Status);

public class CreateWalkInAppointmentHandler(AppDbContext dbContext, INotificationService notificationService)
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

        // 3. Xác định hồ sơ bệnh nhân, theo thứ tự ưu tiên:
        //    a) hồ sơ staff đã chọn từ ô tra cứu;
        //    b) hồ sơ khớp số điện thoại — dò cả cột PhoneNumber của Patient (bệnh nhân tạo tại quầy,
        //       không có tài khoản) lẫn số của tài khoản liên kết;
        //    c) chưa có thì tạo mới.
        Patient? patient = null;

        if (cmd.PatientId is { } patientId)
        {
            patient = await dbContext.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == patientId, ct)
                ?? throw new ValidationException("Không tìm thấy hồ sơ bệnh nhân đã chọn.");
        }
        else
        {
            patient = await dbContext.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p =>
                    p.PhoneNumber == cmd.PatientPhone ||
                    (p.User != null && p.User.PhoneNumber == cmd.PatientPhone), ct);
        }

        if (patient == null)
        {
            var placeholderUser = User.CreateEmployee($"walkin-{Guid.NewGuid()}@songiangdental.com", "Patient", cmd.PatientPhone, cmd.PatientName);
            dbContext.Users.Add(placeholderUser);
            patient = Patient.Create(placeholderUser.Id, cmd.DateOfBirth, cmd.Gender, phoneNumber: cmd.PatientPhone);
            dbContext.Patients.Add(patient);
        }
        else
        {
            // Cập nhật thông tin bệnh nhân tìm được bằng thông tin staff nhập tại quầy
            patient.SetPhoneNumber(cmd.PatientPhone);
            patient.SetDateOfBirth(cmd.DateOfBirth);
            patient.SetGender(cmd.Gender);
            patient.SetFullName(cmd.PatientName);
        }

        // 4. Bệnh nhân đã có mặt tại quầy nên bỏ qua cả Pending lẫn Confirmed:
        //    lịch hẹn vào thẳng CheckedIn để xuất hiện ngay ở hàng đợi, không phải check-in lại.
        var appointment = Appointment.Create(
            patient.Id, cmd.DentistId, cmd.AppointmentDate,
            symptoms: cmd.Symptoms, serviceId: cmd.ServiceId);
        appointment.CheckIn();

        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(ct);

        var code = $"DK{cmd.AppointmentDate:yyyyMMdd}{appointment.Id.ToString("N")[..6].ToUpper()}";

        // Bệnh nhân đã ngồi chờ tại quầy (CheckedIn ngay) nên bác sĩ càng cần được báo ngay lập tức —
        // không báo Staff vì chính nhân viên đang thao tác đã biết rõ việc này rồi.
        var dentistUserId = await dbContext.Dentists
            .Where(d => d.Id == cmd.DentistId)
            .Select(d => (Guid?)d.UserId)
            .FirstOrDefaultAsync(ct);
        if (dentistUserId.HasValue)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: dentistUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.High,
                Title: "Bệnh nhân vãng lai mới",
                Body: $"{cmd.PatientName} vừa đến khám và đang chờ tại quầy.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointment.Id.ToString()), ct);
        }

        return new CreateWalkInResult(appointment.Id, code, patient.FullName, appointment.Status.ToString());
    }
}
