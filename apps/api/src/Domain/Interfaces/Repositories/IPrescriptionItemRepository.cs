using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

/// <summary>Từng loại thuốc trong đơn thuốc.</summary>
public interface IPrescriptionItemRepository
{
    Task<PrescriptionItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(PrescriptionItem item, CancellationToken ct = default);
    Task UpdateAsync(PrescriptionItem item, CancellationToken ct = default);
    Task DeleteAsync(PrescriptionItem item, CancellationToken ct = default);

    /// <summary>
    /// Các dòng thuốc đủ dữ liệu để sinh lịch nhắc uống (có TimesPerDay/DurationDays/StartDate)
    /// của chính bệnh nhân và các thành viên gia đình do bệnh nhân này quản lý.
    /// Đọc kèm Prescription → Appointment → Patient để biết uống thuốc cho ai.
    /// </summary>
    Task<IReadOnlyList<PrescriptionItem>> GetActiveMedicationRemindersByPatientAsync(
        Guid patientId, CancellationToken ct = default);
}
