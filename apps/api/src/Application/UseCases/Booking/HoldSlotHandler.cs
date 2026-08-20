using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Schedules;

namespace DentalClinic.API.Application.UseCases.Booking;

public record HoldSlotCommand(
    Guid? PatientId,
    Guid DentistId,
    DateOnly Date,
    string TimeSlot,
    Guid? ServiceId = null);

public record HoldSlotResult(
    bool IsSuccess,
    Guid HoldId,
    DateTimeOffset ExpiresAt,
    int RemainingSeconds,
    int FailedHoldsToday,
    string Message);

public class HoldSlotHandler(
    ISlotHoldRepository slotHoldRepository,
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository,
    IServiceRepository serviceRepository,
    ICurrentUserService currentUser,
    ISlotNotifier slotNotifier)
{
    public async Task<HoldSlotResult> Handle(HoldSlotCommand command, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var userId = currentUser.UserId ?? Guid.Empty;

        // 1. Xác định hồ sơ bệnh nhân chính chủ hoặc thành viên gia đình
        var myPatient = userId != Guid.Empty ? await patientRepository.GetByUserIdAsync(userId, ct) : null;
        if (myPatient is null && userId != Guid.Empty && currentUser.IsAuthenticated && currentUser.UserRole == "Patient")
        {
            myPatient = Patient.Create(userId: userId, dateOfBirth: null);
            await patientRepository.AddAsync(myPatient, ct);
        }

        var targetPatientId = myPatient?.Id ?? command.PatientId ?? Guid.Empty;
        if (command.PatientId.HasValue && command.PatientId.Value != Guid.Empty)
        {
            if (myPatient != null && command.PatientId.Value != myPatient.Id)
            {
                var family = await patientRepository.GetFamilyMembersAsync(myPatient.Id, ct);
                if (!family.Any(f => f.Id == command.PatientId.Value))
                    throw new ForbiddenException("Bạn không có quyền thao tác trên hồ sơ bệnh nhân này.");
            }
            targetPatientId = command.PatientId.Value;
        }

        if (targetPatientId == Guid.Empty)
            throw new ValidationException("Không xác định được thông tin bệnh nhân.");

        // 2. Phân tích giờ khám & thời lượng ước tính của dịch vụ (Estimate Time)
        var timePart = command.TimeSlot.Split(" - ")[0].Trim();
        var time = TimeOnly.Parse(timePart);
        var apptDateTime = command.Date.ToDateTime(time);
        var apptDateUtc = new DateTimeOffset(apptDateTime, TimeSpan.FromHours(7)).ToUniversalTime();

        if (apptDateUtc <= now)
            throw new ConflictException("Không thể giữ chỗ cho ca khám trong quá khứ.");

        var service = command.ServiceId.HasValue ? await serviceRepository.GetByIdAsync(command.ServiceId.Value, ct) : null;
        var durationMinutes = (service != null && service.DurationMinutes > 0) ? service.DurationMinutes : SlotCalculator.SlotMinutes;

        var slotStartMinutes = time.Hour * 60 + time.Minute;
        var slotEndMinutes = slotStartMinutes + durationMinutes;

        // 3. Kiểm tra số lần giữ không thành công trong ngày (tối đa 3 lần)
        var failedCount = await slotHoldRepository.GetFailedHoldCountTodayAsync(targetPatientId, now, ct);
        if (failedCount >= 3)
            throw new ConflictException(
                "Bệnh nhân đã đạt giới hạn 3 lần giữ chỗ không thành công trong ngày. " +
                "Vui lòng quay lại vào ngày mai.");

        // 3.1. Kiểm tra nếu bệnh nhân đang trong thời gian chờ (cooldown 30 phút sau khi hủy/dời từ lần 2)
        var cooldownUntil = await appointmentRepository.GetPatientCooldownUntilAsync(targetPatientId, now, ct);
        if (cooldownUntil.HasValue && cooldownUntil.Value > now)
        {
            var remaining = (int)Math.Ceiling((cooldownUntil.Value - now).TotalMinutes);
            throw new ConflictException($"Bệnh nhân đang trong thời gian chờ sau khi đổi/hủy lịch. Vui lòng thử lại sau {remaining} phút.");
        }

        // 3.2. Kiểm tra giới hạn tối đa 2 lịch hẹn đang hoạt động của tài khoản
        if (userId != Guid.Empty)
        {
            var activeCount = await appointmentRepository.CountActiveAppointmentsForUserAsync(userId, excludeAppointmentId: null, ct);
            if (activeCount >= 2)
            {
                throw new ConflictException("Tài khoản của bạn đã đạt giới hạn tối đa 2 lịch hẹn đang hoạt động. Vui lòng hoàn thành hoặc dời/hủy bớt lịch hẹn trước khi đặt thêm.");
            }
        }

        // 3.3. Kiểm tra mỗi bệnh nhân chỉ được đặt tối đa 1 lịch hẹn trong 1 ngày
        var hasActiveAppointmentOnDate = await appointmentRepository.HasActiveAppointmentOnDateAsync(
            targetPatientId, command.Date, excludeAppointmentId: null, ct);
        if (hasActiveAppointmentOnDate)
        {
            throw new ConflictException("Bệnh nhân này đã có một lịch hẹn trong ngày này. Mỗi bệnh nhân chỉ được đặt tối đa 1 lịch hẹn mỗi ngày (nếu muốn đổi giờ, vui lòng dời hoặc hủy lịch cũ).");
        }

        // 4. Kiểm tra ca khám (kèm thời lượng dịch vụ) có bị trùng với Lịch hẹn chính thức không
        var appointments = await appointmentRepository.GetByDateAsync(command.Date, ct);
        var appointmentRanges = appointments
            .Where(a => a.DentistId == command.DentistId
                     && a.Status != AppointmentStatus.Cancelled
                     && a.Status != AppointmentStatus.NoShow)
            .Select(a =>
            {
                var local = a.AppointmentDate.UtcDateTime.AddHours(7);
                return SlotCalculator.BuildOccupiedRange(local.Hour, local.Minute, a.Service?.DurationMinutes);
            });

        if (SlotCalculator.IsOccupied(slotStartMinutes, slotEndMinutes, appointmentRanges))
        {
            if (durationMinutes > 30)
            {
                throw new ConflictException(
                    $"Dịch vụ này ước tính khoảng ~{durationMinutes} phút. Không thể đổi sang khung giờ {command.TimeSlot} do bị trùng với một lịch hẹn tiếp theo. Vui lòng chọn giờ khác.");
            }
            throw new ConflictException("Ca khám này (hoặc thời lượng dịch vụ) bị trùng với một lịch hẹn đã đặt.");
        }

        DateTimeOffset? inheritedExpiresAt = null;

        // 5. Kiểm tra các lượt giữ đang active của user / bệnh nhân (bất kể nha sĩ hay ngày nào)
        var myActiveHolds = await slotHoldRepository.GetActiveHoldsForUserOrPatientAsync(userId, targetPatientId, now, ct);

        AppointmentSlotHold? sameSlotHold = null;

        foreach (var userHold in myActiveHolds)
        {
            if (userHold.DentistId == command.DentistId && userHold.AppointmentDate == apptDateUtc)
            {
                inheritedExpiresAt ??= userHold.ExpiresAt;
                sameSlotHold = userHold;
            }
            else
            {
                // Khác nha sĩ / ngày / giờ -> giải phóng ngay lập tức
                inheritedExpiresAt ??= userHold.ExpiresAt;
                userHold.Release();
                await slotHoldRepository.UpdateAsync(userHold, ct);

                var oldDateOnly = DateOnly.FromDateTime(userHold.AppointmentDate.ToOffset(TimeSpan.FromHours(7)).DateTime);
                await slotNotifier.NotifySlotReleasedAsync(
                    userHold.DentistId,
                    oldDateOnly,
                    userHold.TimeSlot,
                    ct);
            }
        }

        // 6. Kiểm tra xem ca khám (kèm thời lượng dịch vụ) có bị người khác giữ không
        var dentistActiveHolds = await slotHoldRepository.GetActiveHoldsForDentistAndDateAsync(command.DentistId, command.Date, now, ct);
        var otherHoldRanges = dentistActiveHolds
            .Where(h => h.PatientId != targetPatientId && (userId == Guid.Empty || h.UserId != userId))
            .Select(h =>
            {
                var local = h.AppointmentDate.UtcDateTime.AddHours(7);
                return SlotCalculator.BuildOccupiedRange(local.Hour, local.Minute, h.DurationMinutes > 0 ? h.DurationMinutes : 30);
            });

        if (SlotCalculator.IsOccupied(slotStartMinutes, slotEndMinutes, otherHoldRanges))
        {
            if (durationMinutes > 30)
            {
                throw new ConflictException(
                    $"Dịch vụ này ước tính khoảng ~{durationMinutes} phút. Không thể đổi sang khung giờ {command.TimeSlot} do bị trùng với ca đang được giữ tạm tiếp theo. Vui lòng chọn giờ khác.");
            }
            throw new ConflictException(
                "Ca khám này (hoặc thời lượng dịch vụ) đang được một bệnh nhân khác giữ tạm (tối đa 5 phút). " +
                "Vui lòng chọn ca khám khác hoặc quay lại sau.");
        }

        // 7. Nếu đang giữ chính ca này -> cập nhật Service/Patient nếu có thay đổi và trả về
        if (sameSlotHold != null)
        {
            sameSlotHold.UpdateService(command.ServiceId, durationMinutes);
            sameSlotHold.UpdatePatient(targetPatientId);
            await slotHoldRepository.UpdateAsync(sameSlotHold, ct);

            await slotNotifier.NotifySlotHeldAsync(
                command.DentistId,
                command.Date,
                command.TimeSlot,
                targetPatientId,
                sameSlotHold.ExpiresAt,
                ct);

            var remaining = (int)Math.Max(0, (sameSlotHold.ExpiresAt - now).TotalSeconds);
            return new HoldSlotResult(
                true,
                sameSlotHold.Id,
                sameSlotHold.ExpiresAt,
                remaining,
                failedCount,
                "Bạn đang giữ chỗ ca khám này.");
        }

        // 8. Tạo lượt giữ chỗ mới: kế thừa expiresAt của phiên giữ trước (không reset về 5 phút)
        var expiresAt = (inheritedExpiresAt.HasValue && inheritedExpiresAt.Value > now)
            ? inheritedExpiresAt.Value
            : now.AddMinutes(5);

        var newHold = AppointmentSlotHold.CreateWithExpiry(
            targetPatientId,
            userId,
            command.DentistId,
            apptDateUtc,
            command.TimeSlot,
            now,
            expiresAt,
            command.ServiceId,
            durationMinutes);

        await slotHoldRepository.AddAsync(newHold, ct);

        // 9. Broadcast realtime event qua ISlotNotifier
        await slotNotifier.NotifySlotHeldAsync(
            command.DentistId,
            command.Date,
            command.TimeSlot,
            targetPatientId,
            newHold.ExpiresAt,
            ct);

        var remainingSeconds = (int)Math.Max(0, (newHold.ExpiresAt - now).TotalSeconds);

        return new HoldSlotResult(
            true,
            newHold.Id,
            newHold.ExpiresAt,
            remainingSeconds,
            failedCount,
            "Giữ chỗ thành công.");
    }
}
