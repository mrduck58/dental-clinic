using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Booking;

/// <summary>
/// Logic dùng chung của các handler đổi trạng thái lịch hẹn — trước đây là private member của
/// god-handler <c>UpdateAppointmentStatusHandler</c> (7 method), nay được tách thành
/// Confirm/Cancel/CheckIn/MarkNoShow (Booking) và StartTreatment/Complete/EndTreatment (ClinicalRecords).
/// Để static (không cần đăng ký DI) vì chỉ là hàm thuần dựa trên repository truyền vào.
/// </summary>
public static class AppointmentStatusHelper
{
    public static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    /// <summary>
    /// Tài khoản nhận thông báo của một hồ sơ bệnh nhân: thành viên gia đình không có tài khoản
    /// riêng nên thông báo phải về tài khoản chủ hộ (PrimaryPatientId).
    /// </summary>
    public static async Task<Guid?> GetPatientUserIdAsync(
        IPatientRepository? patientRepository, Guid patientId, CancellationToken ct)
    {
        if (patientRepository == null) return null;
        var patient = await patientRepository.GetByIdAsync(patientId, ct);
        if (patient == null) return null;
        if (patient.PrimaryPatientId.HasValue)
        {
            var primary = await patientRepository.GetByIdAsync(patient.PrimaryPatientId.Value, ct);
            return primary?.UserId ?? patient.UserId;
        }
        return patient.UserId;
    }
}
