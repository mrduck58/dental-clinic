using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.Booking;

/// <summary>Kết quả xét quyền — <paramref name="IsPatientCaller"/> quyết định có áp các giới hạn
/// dành riêng cho bệnh nhân hay không (hạn 24 giờ, số lần dời, phải xác nhận lại).</summary>
public record AppointmentChangeContext(bool IsPatientCaller);

/// <summary>
/// Xét quyền hủy/dời một lịch hẹn — dùng chung bởi CancelAppointmentHandler và
/// RescheduleAppointmentHandler vì hai luồng có cùng bộ quy tắc.
///
/// Các giới hạn (hạn chót, số lần dời) CHỈ áp cho bệnh nhân. Khi lễ tân thao tác thì chính họ đang
/// là người sắp xếp lịch — bắt họ tuân theo hạn 24 giờ sẽ khiến việc xử lý cuộc gọi phút chót thành
/// bất khả thi, đúng lúc cần nhất.
/// </summary>
public class AppointmentChangeGuard(
    ICurrentUserService currentUser,
    IPatientRepository patientRepository,
    IAppointmentRepository appointmentRepository)
{
    /// <summary>Bệnh nhân chỉ được tự hủy/dời lịch trong vòng 24 giờ kể từ thời điểm đặt lịch.</summary>
    public static readonly TimeSpan PatientSelfManagementPeriod = TimeSpan.FromHours(24);

    /// <summary>Số lần một bệnh nhân được tự dời cùng một lịch hẹn.</summary>
    public const int MaxPatientReschedules = 2;

    public async Task<AppointmentChangeContext> AuthorizeAsync(
        Appointment appointment, DateTimeOffset now, CancellationToken ct)
    {
        var isPatient = currentUser.IsAuthenticated && currentUser.UserRole == "Patient";

        if (!isPatient)
            return new AppointmentChangeContext(IsPatientCaller: false);

        await EnsureOwnsAppointmentAsync(appointment, ct);

        // 1. Kiểm tra nếu đã đến hoặc qua giờ khám
        if (now >= appointment.AppointmentDate)
            throw new ConflictException(
                "Lịch khám đã đến hoặc đã qua giờ hẹn, không thể tự hủy hoặc dời lịch. " +
                "Vui lòng liên hệ phòng khám để được hỗ trợ.");

        // 2. Kiểm tra nếu đã quá 24 giờ kể từ thời điểm tạo lịch
        if (now - appointment.CreatedAt > PatientSelfManagementPeriod)
            throw new ConflictException(
                "Đã quá 24 giờ kể từ thời điểm đặt lịch, bạn không thể tự hủy hoặc dời lịch. " +
                "Vui lòng liên hệ phòng khám để được hỗ trợ.");

        // 3. Kiểm tra nếu bệnh nhân đang trong thời gian chờ (cooldown 30 phút sau khi hủy/dời từ lần 2)
        var cooldownUntil = await appointmentRepository.GetPatientCooldownUntilAsync(appointment.PatientId, now, ct);
        if (cooldownUntil.HasValue && cooldownUntil.Value > now)
        {
            var remaining = (int)Math.Ceiling((cooldownUntil.Value - now).TotalMinutes);
            throw new ConflictException(
                $"Bệnh nhân đang trong thời gian chờ sau khi đổi/hủy lịch. " +
                $"Vui lòng thử lại sau {remaining} phút.");
        }

        return new AppointmentChangeContext(IsPatientCaller: true);
    }

    /// <summary>
    /// Lịch hẹn phải thuộc hồ sơ chính chủ hoặc một thành viên gia đình dưới tài khoản đó
    /// (chủ hộ đặt lịch hộ vợ/con là luồng bình thường).
    ///
    /// Lịch của người khác trả 404 chứ không 403 — cùng quy ước với các guard quyền sở hữu khác
    /// trong dự án: 403 xác nhận "id này có thật", đủ để dò ra lịch hẹn của người lạ.
    /// </summary>
    private async Task EnsureOwnsAppointmentAsync(Appointment appointment, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Không xác định được người dùng từ token.");

        var patient = await patientRepository.GetByUserIdAsync(userId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.PatientId == patient.Id) return;

        var familyMembers = await patientRepository.GetFamilyMembersAsync(patient.Id, ct);
        if (familyMembers.Any(f => f.Id == appointment.PatientId)) return;

        throw new NotFoundException("Không tìm thấy lịch hẹn.");
    }
}
