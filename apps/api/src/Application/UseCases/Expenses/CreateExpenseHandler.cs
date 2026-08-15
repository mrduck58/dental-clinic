using DentalClinic.API.Application.DTOs.Expenses;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Expenses;

public record CreateExpenseCommand(CreateExpenseRequest Request) : IRequest<ExpenseDto>;

public class CreateExpenseHandler(
    IExpenseRepository expenseRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<CreateExpenseCommand, ExpenseDto>
{
    public async Task<ExpenseDto> Handle(CreateExpenseCommand command, CancellationToken ct)
    {
        var r = command.Request;
        if (!Enum.TryParse<ExpenseCategory>(r.Category, true, out var category))
            throw new ValidationException("Danh mục chi phí không hợp lệ.");

        RecurrenceFrequency? frequency = null;
        if (r.IsRecurring)
        {
            if (!Enum.TryParse<RecurrenceFrequency>(r.Frequency, true, out var freq))
                throw new ValidationException("Chu kỳ lặp lại không hợp lệ.");
            frequency = freq;
        }

        var expense = Expense.Create(category, r.Description, r.Amount, r.Date, r.Note, r.IsRecurring, frequency);
        await expenseRepository.AddAsync(expense, ct);
        await expenseRepository.SaveChangesAsync(ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Expense,
            description: $"Thêm chi phí: {expense.Description} ({expense.Amount:#,##0}đ)",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: expense.Id.ToString(),
            ct: ct);

        return ExpenseMapper.ToDto(expense);
    }
}
