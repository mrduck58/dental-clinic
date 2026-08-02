using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Reminders;

public record MedicationReminderDto(
    Guid PrescriptionItemId,
    string MedicineName,
    string Dosage,
    string Usage,
    string Time,
    Guid PatientId,
    string PatientName,
    string PatientRelationship);

public record GetMedicationRemindersQuery(Guid UserId, DateOnly Date) : IRequest<List<MedicationReminderDto>>;

/// <summary>
/// Sinh lịch nhắc uống thuốc THẬT từ đơn thuốc đang trong thời gian sử dụng (StartDate..StartDate+DurationDays)
/// của bệnh nhân hiện tại và thành viên gia đình. Đơn thuốc không được bác sĩ nhập TimesPerDay/DurationDays
/// sẽ không sinh nhắc nhở (không có đủ dữ liệu để suy ra lịch một cách đáng tin cậy).
/// </summary>
public class GetMedicationRemindersHandler(
    IPatientRepository patientRepository,
    IPrescriptionItemRepository prescriptionItemRepository)
    : IRequestHandler<GetMedicationRemindersQuery, List<MedicationReminderDto>>
{
    public async Task<List<MedicationReminderDto>> Handle(GetMedicationRemindersQuery request, CancellationToken ct)
    {
        var date = request.Date;

        var patient = await patientRepository.GetByUserIdAsync(request.UserId, ct);
        if (patient is null) return [];

        var items = await prescriptionItemRepository.GetActiveMedicationRemindersByPatientAsync(patient.Id, ct);

        var result = new List<MedicationReminderDto>();
        foreach (var item in items)
        {
            var start = item.StartDate!.Value;
            var end = start.AddDays(item.DurationDays!.Value);
            if (date < start || date >= end) continue;

            var appointmentPatient = item.Prescription.Appointment.Patient;
            var relationship = appointmentPatient.Id == patient.Id ? "Tôi" : (appointmentPatient.Relationship ?? string.Empty);

            foreach (var time in GenerateTimeSlots(item.TimesPerDay!.Value))
            {
                result.Add(new MedicationReminderDto(
                    item.Id, item.MedicineName, item.Dosage, item.Usage, time,
                    appointmentPatient.Id, appointmentPatient.FullName, relationship));
            }
        }

        return result.OrderBy(r => r.Time).ToList();
    }

    /// <summary>Dàn đều các lần uống trong khung giờ thức (07:00-21:00) — đơn thuốc chỉ ghi số lần/ngày,
    /// không có giờ cụ thể, nên đây là giờ gợi ý chứ không phải giờ bác sĩ chỉ định chính xác.</summary>
    private static List<string> GenerateTimeSlots(int timesPerDay)
    {
        if (timesPerDay <= 1) return ["08:00"];

        const int startHour = 7;
        const int endHour = 21;
        var stepMinutes = (endHour - startHour) * 60 / (timesPerDay - 1);

        var slots = new List<string>();
        for (var i = 0; i < timesPerDay; i++)
        {
            var totalMinutes = startHour * 60 + stepMinutes * i;
            slots.Add($"{totalMinutes / 60:D2}:{totalMinutes % 60:D2}");
        }
        return slots;
    }
}
