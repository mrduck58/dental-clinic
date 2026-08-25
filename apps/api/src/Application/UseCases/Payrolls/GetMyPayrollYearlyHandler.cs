using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Payrolls;

/// <summary>Diễn biến lương 12 tháng của chính người đang đăng nhập (Dentist/Staff) trong một năm.</summary>
public record GetMyPayrollYearlyQuery(Guid UserId, int Year) : IRequest<MyPayrollYearlyDto>;

public class GetMyPayrollYearlyHandler(IPayrollRepository payrollRepository, IWorkScheduleRepository workScheduleRepository)
    : IRequestHandler<GetMyPayrollYearlyQuery, MyPayrollYearlyDto>
{
    public async Task<MyPayrollYearlyDto> Handle(GetMyPayrollYearlyQuery query, CancellationToken ct)
    {
        if (query.Year is < 2000 or > 2200)
            throw new ValidationException("Năm báo cáo không hợp lệ.");

        var user = await payrollRepository.GetPayableUserByIdAsync(query.UserId, ct);
        if (user is null)
            return new MyPayrollYearlyDto(query.Year, 0m, 0, []);

        var records = await payrollRepository.GetByUserAndYearAsync(user.Id, query.Year, ct);
        var recordByMonth = records.ToDictionary(r => r.Month);
        var yearStart = new DateOnly(query.Year, 1, 1);
        var yearEnd = new DateOnly(query.Year, 12, 31);
        var leaves = await payrollRepository.GetApprovedLeavesOverlappingAsync(yearStart, yearEnd, ct);
        var employeeId = user.Employee?.Id ?? Guid.Empty;
        var yearSchedules = await workScheduleRepository.GetByDateRangeAsync(yearStart, yearEnd, ct);
        var shiftsByMonth = yearSchedules
            .Where(s => !s.IsHoliday && s.EmployeeId == employeeId)
            .GroupBy(s => s.Date.Month)
            .ToDictionary(g => g.Key, g => g.Count());

        var months = new List<MyPayrollMonthDto>(12);
        for (var month = 1; month <= 12; month++)
        {
            var record = recordByMonth.GetValueOrDefault(month);
            if (record is not null)
            {
                months.Add(new MyPayrollMonthDto(month, record.NetSalary, record.Status.ToString(), record.PaidAt));
            }
            else
            {
                var requiredShifts = shiftsByMonth.GetValueOrDefault(month, 0);
                var c = PayrollCalculator.Compute(user, leaves, requiredShifts, query.Year, month);
                months.Add(new MyPayrollMonthDto(month, c.NetSalary, "NotCreated", null));
            }
        }

        return new MyPayrollYearlyDto(
            Year: query.Year,
            TotalNet: months.Sum(m => m.NetSalary),
            PaidCount: months.Count(m => m.Status == nameof(PayrollStatus.Paid)),
            Months: months);
    }
}
