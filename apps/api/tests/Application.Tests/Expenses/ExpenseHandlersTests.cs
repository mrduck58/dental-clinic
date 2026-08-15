using DentalClinic.API.Application.DTOs.Expenses;
using DentalClinic.API.Application.UseCases.Expenses;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Expenses;

[TestFixture]
public class ExpenseHandlersTests
{
    private IExpenseRepository _repo = null!;
    private IActivityLogService _activityLog = null!;
    private ICurrentUserService _currentUser = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IExpenseRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
    }

    private static CreateExpenseRequest MakeCreateRequest(
        string category = "Rent", decimal amount = 5_000_000m, bool isRecurring = false, string? frequency = null)
        => new(category, "Tiền thuê mặt bằng tháng 8", amount, new DateOnly(2026, 8, 1), null, isRecurring, frequency);

    // ── CreateExpenseHandler ─────────────────────────────────────────────────

    [Test]
    public async Task Create_ValidRequest_ReturnsExpenseDtoWithMatchingFields()
    {
        var handler = new CreateExpenseHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(new CreateExpenseCommand(MakeCreateRequest()), CancellationToken.None);

        result.Category.Should().Be("Rent");
        result.Amount.Should().Be(5_000_000m);
        result.IsRecurring.Should().BeFalse();
        await _repo.Received(1).AddAsync(Arg.Any<Expense>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Create_InvalidCategory_ThrowsValidationException()
    {
        var handler = new CreateExpenseHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(new CreateExpenseCommand(MakeCreateRequest(category: "NotACategory")), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Create_RecurringWithoutFrequency_ThrowsValidationException()
    {
        var handler = new CreateExpenseHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(
            new CreateExpenseCommand(MakeCreateRequest(isRecurring: true, frequency: null)), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Create_RecurringWithFrequency_SetsFrequencyOnDto()
    {
        var handler = new CreateExpenseHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(
            new CreateExpenseCommand(MakeCreateRequest(isRecurring: true, frequency: "Monthly")), CancellationToken.None);

        result.IsRecurring.Should().BeTrue();
        result.Frequency.Should().Be("Monthly");
    }

    // ── UpdateExpenseHandler ─────────────────────────────────────────────────

    [Test]
    public async Task Update_ExistingExpense_UpdatesFieldsAndReturnsDto()
    {
        var expense = Expense.Create(ExpenseCategory.Software, "Phần mềm quản lý cũ", 1_000_000m, new DateOnly(2026, 8, 1), null, false, null);
        _repo.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);
        var handler = new UpdateExpenseHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(
            new UpdateExpenseCommand(expense.Id, new UpdateExpenseRequest("Software", "Phần mềm quản lý mới", 1_500_000m, new DateOnly(2026, 8, 5), "Ghi chú", false, null)),
            CancellationToken.None);

        result.Description.Should().Be("Phần mềm quản lý mới");
        result.Amount.Should().Be(1_500_000m);
    }

    [Test]
    public async Task Update_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Expense?)null);
        var handler = new UpdateExpenseHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(
            new UpdateExpenseCommand(Guid.NewGuid(), new UpdateExpenseRequest("Other", "x", 1000m, new DateOnly(2026, 8, 1), null, false, null)),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── DeleteExpenseHandler ─────────────────────────────────────────────────

    [Test]
    public async Task Delete_ExistingExpense_CallsDeleteAsync()
    {
        var expense = Expense.Create(ExpenseCategory.Other, "Chi phí linh tinh", 200_000m, new DateOnly(2026, 8, 1), null, false, null);
        _repo.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);
        var handler = new DeleteExpenseHandler(_repo, _activityLog, _currentUser);

        await handler.Handle(new DeleteExpenseCommand(expense.Id), CancellationToken.None);

        await _repo.Received(1).DeleteAsync(expense, Arg.Any<CancellationToken>());
    }

    // ── GenerateRecurringExpensesHandler ─────────────────────────────────────

    [Test]
    public async Task GenerateRecurring_TemplateWithNoInstanceYet_GeneratesOne()
    {
        var template = Expense.Create(ExpenseCategory.Rent, "Thuê mặt bằng", 5_000_000m, new DateOnly(2026, 7, 1), null, true, RecurrenceFrequency.Monthly);
        _repo.GetActiveRecurringTemplatesAsync(Arg.Any<CancellationToken>()).Returns(new List<Expense> { template });
        _repo.HasRecurrenceInstanceInPeriodAsync(template.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = new GenerateRecurringExpensesHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(new GenerateRecurringExpensesCommand(new DateOnly(2026, 8, 15)), CancellationToken.None);

        result.GeneratedCount.Should().Be(1);
        await _repo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<Expense>>(list => list.Count() == 1 && list.First().RecurringSourceId == template.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GenerateRecurring_TemplateAlreadyHasInstanceThisPeriod_SkipsIt()
    {
        var template = Expense.Create(ExpenseCategory.Rent, "Thuê mặt bằng", 5_000_000m, new DateOnly(2026, 7, 1), null, true, RecurrenceFrequency.Monthly);
        _repo.GetActiveRecurringTemplatesAsync(Arg.Any<CancellationToken>()).Returns(new List<Expense> { template });
        _repo.HasRecurrenceInstanceInPeriodAsync(template.Id, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new GenerateRecurringExpensesHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(new GenerateRecurringExpensesCommand(new DateOnly(2026, 8, 15)), CancellationToken.None);

        result.GeneratedCount.Should().Be(0);
        await _repo.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<Expense>>(), Arg.Any<CancellationToken>());
    }
}
