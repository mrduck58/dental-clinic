using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Booking;

public record StaffAppointmentDto(
    Guid AppointmentId,
    string AppointmentCode,
    Guid PatientId,
    string PatientName,
    string? PatientPhone,
    string DentistName,
    string? ServiceName,
    DateTimeOffset AppointmentDate,
    DateTimeOffset CreatedAt,
    string Status,
    string? Symptoms,
    DateTimeOffset? CheckedInAt,
    /// <summary>"Online" (bệnh nhân tự đặt) hoặc "WalkIn" (lễ tân lập tại quầy).</summary>
    string Origin,
    /// <summary>Quan hệ với chủ tài khoản (VD: "Con", "Vợ"...) — null nếu bệnh nhân tự đặt cho chính mình.</summary>
    string? PatientRelationship,
    /// <summary>Tên chủ tài khoản đã đặt lịch — chính bệnh nhân nếu tự đặt, hoặc người thân quản lý hồ sơ nếu đặt hộ.</summary>
    string AccountHolderName,
    /// <summary>Email đăng nhập của chủ tài khoản đã đặt lịch.</summary>
    string? AccountHolderEmail);

public record GetAllAppointmentsQuery(DateOnly? Date, string? Status)
    : IRequest<IEnumerable<StaffAppointmentDto>>;

public class GetAllAppointmentsHandler(IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetAllAppointmentsQuery, IEnumerable<StaffAppointmentDto>>
{
    public async Task<IEnumerable<StaffAppointmentDto>> Handle(GetAllAppointmentsQuery request, CancellationToken ct)
    {
        var date = request.Date;
        var status = request.Status;

        AppointmentStatus? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<AppointmentStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            statusEnum = parsedStatus;
        }

        var appointments = await appointmentRepository.GetStaffAppointmentsAsync(date, statusEnum, ct);

        return appointments.Select(a =>
        {
            // Hồ sơ người thân dùng User giả (email placeholder) — chủ tài khoản thật là
            // PrimaryPatient. Bệnh nhân tự đặt thì chính họ là chủ tài khoản.
            var accountHolder = a.Patient.PrimaryPatient ?? a.Patient;
            return new StaffAppointmentDto(
                a.Id,
                $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString()[..6].ToUpper()}",
                a.Patient.Id,
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
        });
    }
}
