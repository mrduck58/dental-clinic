using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Expenses;

public record DeleteExpenseCommand(Guid Id) : IRequest;

public class DeleteExpenseHandler(
    IExpenseRepository expenseRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<DeleteExpenseCommand>
{
    public async Task Handle(DeleteExpenseCommand command, CancellationToken ct)
    {
        var expense = await expenseRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy khoản chi phí.");

        await expenseRepository.DeleteAsync(expense, ct);
        await expenseRepository.SaveChangesAsync(ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Delete,
            module: ActivityModule.Expense,
            description: $"Xoá chi phí: {expense.Description} ({expense.Amount:#,##0}đ)",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: expense.Id.ToString(),
            ct: ct);
    }
}
