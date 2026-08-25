using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Payrolls;

/// <summary>Bảng lương của chính người đang đăng nhập (Dentist/Staff), một kỳ tháng/năm.</summary>
public record GetMyPayrollPeriodQuery(Guid UserId, int Year, int Month) : IRequest<MyPayrollPeriodDto>;

public class GetMyPayrollPeriodHandler(IPayrollRepository payrollRepository, IWorkScheduleRepository workScheduleRepository)
    : IRequestHandler<GetMyPayrollPeriodQuery, MyPayrollPeriodDto>
{
    public async Task<MyPayrollPeriodDto> Handle(GetMyPayrollPeriodQuery query, CancellationToken ct)
    {
        if (query.Month is < 1 or > 12)
            throw new ValidationException("Tháng phải nằm trong khoảng 1–12.");

        var user = await payrollRepository.GetPayableUserByIdAsync(query.UserId, ct);
        if (user is null)
            return new MyPayrollPeriodDto(query.Year, query.Month, PayrollCalculator.WorkingShiftsPerMonth, null);

        var (prevYear, prevMonth) = GetPayrollPeriodHandler.PreviousPeriod(query.Year, query.Month);
        var (prevFrom, prevTo) = GetPayrollPeriodHandler.PeriodRange(prevYear, prevMonth);
        var (from, to) = GetPayrollPeriodHandler.PeriodRange(query.Year, query.Month);

        var record = await payrollRepository.GetByUserAndPeriodAsync(user.Id, query.Year, query.Month, ct);
        var prevRecord = await payrollRepository.GetByUserAndPeriodAsync(user.Id, prevYear, prevMonth, ct);
        // Một truy vấn phủ cả hai kỳ, tránh gọi DB hai lần chỉ để lấy số so sánh
        var leaves = await payrollRepository.GetApprovedLeavesOverlappingAsync(prevFrom, to, ct);
        var shiftCounts = await PayrollShiftCounter.CountByEmployeeAsync(workScheduleRepository, from, to, ct);
        var prevShiftCounts = await PayrollShiftCounter.CountByEmployeeAsync(workScheduleRepository, prevFrom, prevTo, ct);
        var requiredShifts = shiftCounts.GetValueOrDefault(user.Employee?.Id ?? Guid.Empty, 0);
        var prevRequiredShifts = prevShiftCounts.GetValueOrDefault(user.Employee?.Id ?? Guid.Empty, 0);

        var previousNetSalary = GetPayrollPeriodHandler.NetSalaryOf(user, prevRecord, leaves, prevRequiredShifts, prevYear, prevMonth);
        var item = GetPayrollPeriodHandler.BuildItem(user, record, leaves, requiredShifts, query.Year, query.Month, previousNetSalary);

        return new MyPayrollPeriodDto(query.Year, query.Month, PayrollCalculator.WorkingShiftsPerMonth, item);
    }
}
