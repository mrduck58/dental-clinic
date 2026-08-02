using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IPayrollRepository
{
    /// <summary>Các bản ghi lương đã lưu của kỳ (tháng/năm).</summary>
    Task<IReadOnlyList<PayrollRecord>> GetByPeriodAsync(int year, int month, CancellationToken ct = default);

    Task<PayrollRecord?> GetByUserAndPeriodAsync(Guid userId, int year, int month, CancellationToken ct = default);

    /// <summary>Toàn bộ bản ghi lương của một năm (12 kỳ), dùng cho báo cáo năm.</summary>
    Task<IReadOnlyList<PayrollRecord>> GetByYearAsync(int year, CancellationToken ct = default);

    /// <summary>Toàn bộ nhân sự (không tính bệnh nhân) kèm hồ sơ Staff/Dentist để lấy lương.</summary>
    Task<IReadOnlyList<User>> GetPayableUsersAsync(CancellationToken ct = default);

    /// <summary>Đơn nghỉ đã duyệt có khoảng ngày giao với [from, to].</summary>
    Task<IReadOnlyList<LeaveRequest>> GetApprovedLeavesOverlappingAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    Task AddAsync(PayrollRecord record, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<PayrollRecord> records, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
