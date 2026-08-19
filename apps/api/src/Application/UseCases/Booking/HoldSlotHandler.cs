using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.Booking;

public record HoldSlotCommand(
    Guid PatientId,
    Guid DentistId,
    DateOnly Date,
    string TimeSlot);

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
    ICurrentUserService currentUser,
    ISlotNotifier slotNotifier)
{
    public async Task<HoldSlotResult> Handle(HoldSlotCommand command, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var userId = currentUser.UserId ?? Guid.Empty;

        // 1. Kiểm tra quyền của bệnh nhân
        if (currentUser.IsAuthenticated && currentUser.UserRole == "Patient")
        {
            var myPatient = await patientRepository.GetByUserIdAsync(userId, ct);
            if (myPatient != null && myPatient.Id != command.PatientId)
            {
                var family = await patientRepository.GetFamilyMembersAsync(myPatient.Id, ct);
                if (!family.Any(f => f.Id == command.PatientId))
                    throw new ForbiddenException("Bạn không có quyền thao tác trên hồ sơ bệnh nhân này.");
            }
        }

        // 2. Phân tích giờ khám
        var timePart = command.TimeSlot.Split(" - ")[0].Trim();
        var time = TimeOnly.Parse(timePart);
        var apptDateTime = command.Date.ToDateTime(time);
        var apptDateUtc = new DateTimeOffset(apptDateTime, TimeSpan.FromHours(7)).ToUniversalTime();

        if (apptDateUtc <= now)
            throw new ConflictException("Không thể giữ chỗ cho ca khám trong quá khứ.");

        // 3. Kiểm tra số lần giữ không thành công trong ngày (tối đa 3 lần)
        var failedCount = await slotHoldRepository.GetFailedHoldCountTodayAsync(command.PatientId, now, ct);
        if (failedCount >= 3)
            throw new ConflictException(
                "Bệnh nhân đã đạt giới hạn 3 lần giữ chỗ không thành công trong ngày. " +
                "Vui lòng quay lại vào ngày mai.");

        // 4. Kiểm tra ca khám đã có lịch hẹn chính thức chưa
        var appointments = await appointmentRepository.GetByDateAsync(command.Date, ct);
        if (appointments.Any(a => a.DentistId == command.DentistId
                               && a.AppointmentDate == apptDateUtc
                               && a.Status != AppointmentStatus.Cancelled
                               && a.Status != AppointmentStatus.NoShow))
        {
            throw new ConflictException("Ca khám này đã có người đặt.");
        }

        // 5. Kiểm tra ca khám có đang bị người khác giữ không
        var existingHold = await slotHoldRepository.GetActiveHoldForSlotAsync(command.DentistId, apptDateUtc, now, ct);
        if (existingHold != null && existingHold.PatientId != command.PatientId)
        {
            throw new ConflictException(
                "Ca khám này đang được một bệnh nhân khác giữ tạm (tối đa 5 phút). " +
                "Vui lòng chọn ca khám khác hoặc quay lại sau.");
        }

        // 6. Nếu bệnh nhân đang giữ chính ca này -> trả về hạn cũ không gia hạn
        var myActiveHold = await slotHoldRepository.GetActiveHoldForPatientAsync(command.PatientId, now, ct);
        if (myActiveHold != null)
        {
            if (myActiveHold.DentistId == command.DentistId && myActiveHold.AppointmentDate == apptDateUtc)
            {
                var remaining = (int)Math.Max(0, (myActiveHold.ExpiresAt - now).TotalSeconds);
                return new HoldSlotResult(
                    true,
                    myActiveHold.Id,
                    myActiveHold.ExpiresAt,
                    remaining,
                    failedCount,
                    "Bạn đang giữ chỗ ca khám này.");
            }

            // Nếu đổi sang ca khác -> giải phóng ca cũ
            myActiveHold.Release();
            await slotHoldRepository.UpdateAsync(myActiveHold, ct);

            var oldDateOnly = DateOnly.FromDateTime(myActiveHold.AppointmentDate.ToOffset(TimeSpan.FromHours(7)).DateTime);
            await slotNotifier.NotifySlotReleasedAsync(
                myActiveHold.DentistId,
                oldDateOnly,
                myActiveHold.TimeSlot,
                ct);
        }

        // 7. Tạo lượt giữ chỗ mới (hạn 5 phút từ bây giờ)
        var newHold = AppointmentSlotHold.Create(
            command.PatientId,
            userId,
            command.DentistId,
            apptDateUtc,
            command.TimeSlot,
            now);

        await slotHoldRepository.AddAsync(newHold, ct);

        // 8. Broadcast realtime event qua ISlotNotifier
        await slotNotifier.NotifySlotHeldAsync(
            command.DentistId,
            command.Date,
            command.TimeSlot,
            command.PatientId,
            newHold.ExpiresAt,
            ct);

        return new HoldSlotResult(
            true,
            newHold.Id,
            newHold.ExpiresAt,
            300,
            failedCount,
            "Giữ chỗ thành công trong 5 phút.");
    }
}
