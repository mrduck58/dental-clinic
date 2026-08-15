using DentalClinic.API.Application.DTOs.Expenses;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Expenses;

/// <summary>
/// Sinh bản ghi chi phí của kỳ hiện tại (theo ngày hôm nay) cho các mẫu định kỳ đang hoạt động —
/// bỏ qua mẫu nào đã có bản ghi rơi vào đúng kỳ đó rồi (an toàn để gọi lại nhiều lần).
/// </summary>
public record GenerateRecurringExpensesCommand(DateOnly Today) : IRequest<GenerateRecurringExpensesResult>;

public class GenerateRecurringExpensesHandler(
    IExpenseRepository expenseRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<GenerateRecurringExpensesCommand, GenerateRecurringExpensesResult>
{
    public async Task<GenerateRecurringExpensesResult> Handle(GenerateRecurringExpensesCommand command, CancellationToken ct)
    {
        var templates = await expenseRepository.GetActiveRecurringTemplatesAsync(ct);
        var generated = new List<Expense>();

        foreach (var template in templates)
        {
            var (periodStart, periodEnd) = PeriodOf(template.Frequency!.Value, command.Today);
            var alreadyGenerated = await expenseRepository.HasRecurrenceInstanceInPeriodAsync(template.Id, periodStart, periodEnd, ct);
            if (alreadyGenerated) continue;

            generated.Add(Expense.CreateRecurrenceInstance(template, command.Today));
        }

        if (generated.Count > 0)
        {
            await expenseRepository.AddRangeAsync(generated, ct);
            await expenseRepository.SaveChangesAsync(ct);
        }

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Expense,
            description: $"Sinh {generated.Count} chi phí định kỳ cho kỳ hiện tại",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: null,
            ct: ct);

        return new GenerateRecurringExpensesResult(generated.Count);
    }

    private static (DateOnly Start, DateOnly End) PeriodOf(RecurrenceFrequency frequency, DateOnly today) => frequency switch
    {
        RecurrenceFrequency.Monthly => (
            new DateOnly(today.Year, today.Month, 1),
            new DateOnly(today.Year, today.Month, 1).AddMonths(1).AddDays(-1)),
        RecurrenceFrequency.Quarterly => QuarterOf(today),
        RecurrenceFrequency.Yearly => (new DateOnly(today.Year, 1, 1), new DateOnly(today.Year, 12, 31)),
        _ => (today, today),
    };

    private static (DateOnly Start, DateOnly End) QuarterOf(DateOnly today)
    {
        var quarterStartMonth = ((today.Month - 1) / 3) * 3 + 1;
        var start = new DateOnly(today.Year, quarterStartMonth, 1);
        return (start, start.AddMonths(3).AddDays(-1));
    }
}
