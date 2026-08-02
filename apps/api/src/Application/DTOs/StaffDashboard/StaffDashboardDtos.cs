namespace DentalClinic.API.Application.DTOs.StaffDashboard;

/// <summary>4 chỉ số chính của Dashboard Staff: lịch hẹn hôm nay, chờ check-in, đang khám, chờ thanh toán.</summary>
public record StaffDashboardStatsDto(
    int AppointmentsTodayCount,
    int WaitingCheckInCount,
    int InProgressCount,
    int PendingInvoicesCount);

public record StaffTodayAppointmentDto(
    Guid Id,
    string PatientName,
    string? ServiceName,
    string DentistName,
    DateTimeOffset AppointmentDate,
    string Status);

public record StaffPendingInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string PatientName,
    string? ServiceName,
    decimal Amount);
