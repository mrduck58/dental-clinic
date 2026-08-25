using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IWorkScheduleRepository
{
    Task<IEnumerable<WorkSchedule>> GetByWeekAsync(DateOnly weekStart, CancellationToken ct = default);
    Task ReplaceWeekAsync(DateOnly weekStart, IEnumerable<WorkSchedule> entries, CancellationToken ct = default);

    /// <summary>Ca làm việc hợp lệ của bác sĩ (Type="dentist", không nghỉ lễ, mã ca hợp lệ) trong một ngày,
    /// lọc thêm theo phòng nếu <paramref name="room"/> được cung cấp.</summary>
    Task<IReadOnlyList<WorkSchedule>> GetDentistSchedulesForDateAsync(DateOnly date, string? room = null, CancellationToken ct = default);

    /// <summary>Mọi bản ghi lịch làm việc (không lọc loại/ca) của một ngày cụ thể.</summary>
    Task<IReadOnlyList<WorkSchedule>> GetByDateAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>Mọi bản ghi lịch làm việc trong một khoảng ngày (dùng cho tính lịch trống theo tháng).</summary>
    Task<IReadOnlyList<WorkSchedule>> GetByDateRangeAsync(DateOnly start, DateOnly end, CancellationToken ct = default);

    /// <summary>Gỡ hẳn một loạt ca khỏi lịch làm việc (duyệt đơn nghỉ → bỏ trống các ca của người nghỉ).</summary>
    Task RemoveRangeAsync(IEnumerable<WorkSchedule> entries, CancellationToken ct = default);
}
