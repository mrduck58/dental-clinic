using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class PayrollRepository(AppDbContext db) : IPayrollRepository
{
    private static readonly UserRole[] PayrollRoles = [UserRole.Dentist, UserRole.Staff];

    public async Task<IReadOnlyList<PayrollRecord>> GetByPeriodAsync(int year, int month, CancellationToken ct = default)
        => await db.PayrollRecords
            .Where(p => p.Year == year && p.Month == month)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PayrollRecord>> GetByYearAsync(int year, CancellationToken ct = default)
        => await db.PayrollRecords
            .Where(p => p.Year == year)
            .ToListAsync(ct);

    public async Task<PayrollRecord?> GetByUserAndPeriodAsync(Guid userId, int year, int month, CancellationToken ct = default)
        => await db.PayrollRecords
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Year == year && p.Month == month, ct);

    /// <summary>
    /// Chỉ bác sĩ và nhân viên mới có bảng lương. Admin/Owner là tài khoản quản trị hệ thống,
    /// không phải nhân sự hưởng lương, nên bị loại khỏi mọi kỳ lương.
    /// </summary>
    public async Task<IReadOnlyList<User>> GetPayableUsersAsync(CancellationToken ct = default)
        => await db.Users
            .Include(u => u.Employee).ThenInclude(e => e!.DentistProfile)
            .Where(u => PayrollRoles.Contains(u.Role))
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LeaveRequest>> GetApprovedLeavesOverlappingAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
        => await db.LeaveRequests
            .Where(l => l.Status == LeaveStatus.Approved && l.StartDate <= to && l.EndDate >= from)
            .ToListAsync(ct);

    public async Task AddAsync(PayrollRecord record, CancellationToken ct = default)
    {
        await db.PayrollRecords.AddAsync(record, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<PayrollRecord> records, CancellationToken ct = default)
    {
        await db.PayrollRecords.AddRangeAsync(records, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
