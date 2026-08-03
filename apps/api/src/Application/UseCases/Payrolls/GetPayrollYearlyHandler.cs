using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Payrolls;

/// <summary>Báo cáo quỹ lương cả năm: 12 kỳ, mỗi kỳ theo đúng quy tắc của màn hình bảng lương.</summary>
public record GetPayrollYearlyQuery(int Year) : IRequest<PayrollYearlyDto>;

public class GetPayrollYearlyHandler(IPayrollRepository payrollRepository)
    : IRequestHandler<GetPayrollYearlyQuery, PayrollYearlyDto>
{
    public async Task<PayrollYearlyDto> Handle(GetPayrollYearlyQuery query, CancellationToken ct)
    {
        if (query.Year is < 2000 or > 2200)
            throw new ValidationException("Năm báo cáo không hợp lệ.");

        var users = await payrollRepository.GetPayableUsersAsync(ct);
        var records = await payrollRepository.GetByYearAsync(query.Year, ct);
        // Một truy vấn cho cả năm thay vì 12 lần gọi DB
        var leaves = await payrollRepository.GetApprovedLeavesOverlappingAsync(
            new DateOnly(query.Year, 1, 1), new DateOnly(query.Year, 12, 31), ct);

        var recordsByMonth = records
            .GroupBy(r => r.Month)
            .ToDictionary(g => g.Key, g => g.ToDictionary(r => r.UserId));

        var months = new List<PayrollMonthStatDto>(12);

        for (var month = 1; month <= 12; month++)
        {
            var monthRecords = recordsByMonth.GetValueOrDefault(month);
            var staffCount = 0;
            var paidCount = 0;
            var totalNet = 0m;
            var totalPaid = 0m;
            var totalDeduction = 0m;

            foreach (var user in users)
            {
                var record = monthRecords?.GetValueOrDefault(user.Id);
                var isPaid = record is { Status: PayrollStatus.Paid };

                decimal net, deduction;
                if (isPaid)
                {
                    net = record!.NetSalary;
                    deduction = record.Deduction;
                }
                else
                {
                    var c = PayrollCalculator.Compute(user, leaves, query.Year, month);
                    net = c.NetSalary;
                    deduction = c.Deduction;
                }

                staffCount++;
                totalNet += net;
                totalDeduction += deduction;
                if (isPaid)
                {
                    paidCount++;
                    totalPaid += net;
                }
            }

            months.Add(new PayrollMonthStatDto(month, staffCount, paidCount, totalNet, totalPaid, totalDeduction));
        }

        var yearTotalNet = months.Sum(m => m.TotalNet);

        return new PayrollYearlyDto(
            Year: query.Year,
            TotalNet: yearTotalNet,
            TotalPaid: months.Sum(m => m.TotalPaid),
            TotalDeduction: months.Sum(m => m.TotalDeduction),
            AverageMonthlyNet: Math.Round(yearTotalNet / 12, 0, MidpointRounding.AwayFromZero),
            PeakMonth: months.OrderByDescending(m => m.TotalNet).ThenBy(m => m.Month).First().Month,
            Months: months);
    }
}
