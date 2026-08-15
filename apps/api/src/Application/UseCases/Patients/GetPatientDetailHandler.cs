using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Patients;

public record PatientAppointmentHistoryItemDto(
    Guid AppointmentId,
    string AppointmentCode,
    DateTimeOffset AppointmentDate,
    string DentistName,
    string? ServiceName,
    string Status,
    // null = buổi hẹn chưa xuất hóa đơn (chưa đến bước thanh toán)
    string? PaymentStatus,
    bool? IsSettled,
    decimal? TotalAmount);

public record PatientDetailDto(
    Guid Id,
    string FullName,
    string? Phone,
    string? Email,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Address,
    IReadOnlyList<PatientAppointmentHistoryItemDto> Appointments);

public record GetPatientDetailQuery(Guid PatientId) : IRequest<PatientDetailDto>;

public class GetPatientDetailHandler(IPatientRepository patientRepository, IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetPatientDetailQuery, PatientDetailDto>
{
    public async Task<PatientDetailDto> Handle(GetPatientDetailQuery query, CancellationToken ct)
    {
        var patient = await patientRepository.GetByIdAsync(query.PatientId, ct)
            ?? throw new NotFoundException($"Không tìm thấy bệnh nhân với ID '{query.PatientId}'.");

        var appointments = await appointmentRepository.GetByPatientIdWithDetailsAsync(query.PatientId, ct);

        var items = appointments.Select(a =>
        {
            // Buổi hẹn có thể có 2 hóa đơn (đặt cọc + thu phần còn lại) — lấy hóa đơn mới nhất
            // để phản ánh đúng trạng thái thanh toán hiện tại.
            var invoice = a.Invoices.OrderByDescending(i => i.CreatedAt).FirstOrDefault();
            return new PatientAppointmentHistoryItemDto(
                a.Id,
                $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString()[..6].ToUpper()}",
                a.AppointmentDate,
                a.Dentist.FullName,
                a.Service?.Name,
                a.Status.ToString(),
                invoice?.Status.ToString(),
                invoice?.IsSettled,
                invoice?.TotalAmount);
        }).ToList();

        return new PatientDetailDto(
            patient.Id,
            patient.FullName,
            patient.PhoneNumber,
            patient.User?.Email,
            patient.DateOfBirth,
            patient.Gender,
            patient.Address,
            items);
    }
}
