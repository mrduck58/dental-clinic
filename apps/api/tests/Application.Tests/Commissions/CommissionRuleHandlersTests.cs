using DentalClinic.API.Application.DTOs.Commissions;
using DentalClinic.API.Application.UseCases.Commissions;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Commissions;

[TestFixture]
public class CommissionRuleHandlersTests
{
    private ICommissionRuleRepository _repo = null!;
    private IActivityLogService _activityLog = null!;
    private ICurrentUserService _currentUser = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ICommissionRuleRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
    }

    private static CommissionRuleRequest MakeRequest(
        Guid? dentistId = null, string? serviceName = null, decimal rate = 10m,
        DateOnly? from = null, DateOnly? to = null)
        => new(dentistId, serviceName, rate, from ?? new DateOnly(2026, 1, 1), to, "Ghi chú");

    // ── CreateCommissionRuleHandler ──────────────────────────────────────────

    [Test]
    public async Task Create_ValidRequest_AddsRuleAndReturnsId()
    {
        var handler = new CreateCommissionRuleHandler(_repo, _activityLog, _currentUser);

        var id = await handler.Handle(new CreateCommissionRuleCommand(MakeRequest()), CancellationToken.None);

        id.Should().NotBe(Guid.Empty);
        await _repo.Received(1).AddAsync(Arg.Is<CommissionRule>(r => r.RatePercent == 10m), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Create_RateOutOfRange_ThrowsValidationException()
    {
        var handler = new CreateCommissionRuleHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(new CreateCommissionRuleCommand(MakeRequest(rate: 150m)), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Create_EffectiveToBeforeFrom_ThrowsValidationException()
    {
        var handler = new CreateCommissionRuleHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(
            new CreateCommissionRuleCommand(MakeRequest(from: new DateOnly(2026, 8, 1), to: new DateOnly(2026, 1, 1))),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ── UpdateCommissionRuleHandler ───────────────────────────────────────────

    [Test]
    public async Task Update_ExistingRule_UpdatesFields()
    {
        var rule = CommissionRule.Create(null, null, 10m, new DateOnly(2026, 1, 1), null, null);
        _repo.GetByIdAsync(rule.Id, Arg.Any<CancellationToken>()).Returns(rule);
        var handler = new UpdateCommissionRuleHandler(_repo, _activityLog, _currentUser);

        await handler.Handle(new UpdateCommissionRuleCommand(rule.Id, MakeRequest(rate: 15m)), CancellationToken.None);

        rule.RatePercent.Should().Be(15m);
    }

    [Test]
    public async Task Update_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((CommissionRule?)null);
        var handler = new UpdateCommissionRuleHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(new UpdateCommissionRuleCommand(Guid.NewGuid(), MakeRequest()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── ToggleCommissionRuleActiveHandler ────────────────────────────────────

    [Test]
    public async Task ToggleActive_ActiveRule_DeactivatesIt()
    {
        var rule = CommissionRule.Create(null, null, 10m, new DateOnly(2026, 1, 1), null, null);
        rule.IsActive.Should().BeTrue();
        _repo.GetByIdAsync(rule.Id, Arg.Any<CancellationToken>()).Returns(rule);
        var handler = new ToggleCommissionRuleActiveHandler(_repo, _activityLog, _currentUser);

        await handler.Handle(new ToggleCommissionRuleActiveCommand(rule.Id), CancellationToken.None);

        rule.IsActive.Should().BeFalse();
    }

    // ── DeleteCommissionRuleHandler ───────────────────────────────────────────

    [Test]
    public async Task Delete_ExistingRule_CallsDeleteAsync()
    {
        var rule = CommissionRule.Create(null, null, 10m, new DateOnly(2026, 1, 1), null, null);
        _repo.GetByIdAsync(rule.Id, Arg.Any<CancellationToken>()).Returns(rule);
        var handler = new DeleteCommissionRuleHandler(_repo, _activityLog, _currentUser);

        await handler.Handle(new DeleteCommissionRuleCommand(rule.Id), CancellationToken.None);

        await _repo.Received(1).DeleteAsync(rule, Arg.Any<CancellationToken>());
    }
}
