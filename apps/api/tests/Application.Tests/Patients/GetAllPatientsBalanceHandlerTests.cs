using DentalClinic.API.Application.UseCases.Patients;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Patients;

[TestFixture]
public class GetAllPatientsBalanceHandlerTests
{
    private IPatientRepository _patientRepo = null!;
    private ITreatmentPlanRepository _planRepo = null!;
    private GetAllPatientsBalanceHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _patientRepo = Substitute.For<IPatientRepository>();
        _planRepo = Substitute.For<ITreatmentPlanRepository>();
        _handler = new GetAllPatientsBalanceHandler(_patientRepo, _planRepo);

        _planRepo.GetPlanPaidMapAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>());
    }

    /// <summary>Bệnh nhân chưa từng có liệu trình nào vẫn phải xuất hiện, với 0đ đã thu / 0đ còn nợ —
    /// đúng yêu cầu "xem được TẤT CẢ bệnh nhân", không chỉ người đã phát sinh tiền.</summary>
    [Test]
    public async Task Handle_PatientWithNoTreatmentPlans_ReturnsZeroBalance()
    {
        var patient = MakePatient("Chưa điều trị");
        _patientRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([patient]);
        _planRepo.GetAllWithServiceAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetAllPatientsBalanceQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].PatientId.Should().Be(patient.Id);
        result[0].TotalCost.Should().Be(0);
        result[0].AmountPaid.Should().Be(0);
        result[0].RemainingAmount.Should().Be(0);
        result[0].Services.Should().BeEmpty();
        result[0].TreatmentPlanCount.Should().Be(0);
        result[0].LastTreatmentDate.Should().BeNull();
    }

    /// <summary>Chưa thu đồng nào thì còn nợ đúng bằng tổng chi phí liệu trình.</summary>
    [Test]
    public async Task Handle_PlanWithNoPayment_RemainingEqualsTotalCost()
    {
        var patient = MakePatient("Chưa thanh toán");
        var service = MakeService("Trám răng", 500_000m);
        var plan = MakePlan(patient.Id, service, unitPrice: 500_000m, quantity: 1);

        _patientRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([patient]);
        _planRepo.GetAllWithServiceAsync(Arg.Any<CancellationToken>()).Returns([plan]);

        var result = await _handler.Handle(new GetAllPatientsBalanceQuery(), CancellationToken.None);

        result[0].TotalCost.Should().Be(500_000m);
        result[0].AmountPaid.Should().Be(0);
        result[0].RemainingAmount.Should().Be(500_000m);
        result[0].Services.Should().ContainSingle()
            .Which.Should().Match<Application.DTOs.Patients.PatientServiceBalanceDto>(s =>
                s.ServiceId == service.Id && s.ServiceName == "Trám răng" && s.RemainingAmount == 500_000m);
    }

    /// <summary>Đã thu một phần thì còn nợ = chi phí - đã thu, không bao giờ âm dù thu vượt (an toàn dữ liệu).</summary>
    [Test]
    public async Task Handle_PartiallyPaidPlan_RemainingIsCostMinusPaid()
    {
        var patient = MakePatient("Đã cọc");
        var service = MakeService("Nhổ răng khôn", 2_000_000m);
        var plan = MakePlan(patient.Id, service, unitPrice: 2_000_000m, quantity: 1);

        _patientRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([patient]);
        _planRepo.GetAllWithServiceAsync(Arg.Any<CancellationToken>()).Returns([plan]);
        _planRepo.GetPlanPaidMapAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal> { [plan.Id] = 800_000m });

        var result = await _handler.Handle(new GetAllPatientsBalanceQuery(), CancellationToken.None);

        result[0].AmountPaid.Should().Be(800_000m);
        result[0].RemainingAmount.Should().Be(1_200_000m);
    }

    /// <summary>Nhiều liệu trình cùng một dịch vụ (vd nhổ nhiều răng khác nhau) phải được cộng dồn
    /// vào đúng MỘT dòng dịch vụ đó, không tách thành nhiều dòng trùng tên.</summary>
    [Test]
    public async Task Handle_MultiplePlansSameService_AggregatesIntoOneServiceLine()
    {
        var patient = MakePatient("Nhiều liệu trình");
        var service = MakeService("Trám răng", 500_000m);
        var plan1 = MakePlan(patient.Id, service, unitPrice: 500_000m, quantity: 1);
        var plan2 = MakePlan(patient.Id, service, unitPrice: 500_000m, quantity: 1);

        _patientRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([patient]);
        _planRepo.GetAllWithServiceAsync(Arg.Any<CancellationToken>()).Returns([plan1, plan2]);
        _planRepo.GetPlanPaidMapAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal> { [plan1.Id] = 500_000m });

        var result = await _handler.Handle(new GetAllPatientsBalanceQuery(), CancellationToken.None);

        result[0].Services.Should().ContainSingle();
        result[0].TotalCost.Should().Be(1_000_000m);
        result[0].AmountPaid.Should().Be(500_000m);
        result[0].RemainingAmount.Should().Be(500_000m);
        // 2 liệu trình vẫn tính là 2, dù cùng gộp vào 1 dòng dịch vụ — số liệu trình phản ánh đúng số
        // lần đã chỉ định, không phải số dịch vụ khác nhau.
        result[0].TreatmentPlanCount.Should().Be(2);
        result[0].LastTreatmentDate.Should().NotBeNull();
    }

    /// <summary>Danh sách trả về phải sắp theo số còn nợ giảm dần — bệnh nhân nợ nhiều nhất lên đầu.</summary>
    [Test]
    public async Task Handle_MultiplePatients_SortedByRemainingAmountDescending()
    {
        var lowDebt = MakePatient("Nợ ít");
        var highDebt = MakePatient("Nợ nhiều");
        var service = MakeService("Niềng răng", 30_000_000m);
        var lowPlan = MakePlan(lowDebt.Id, service, unitPrice: 1_000_000m, quantity: 1);
        var highPlan = MakePlan(highDebt.Id, service, unitPrice: 30_000_000m, quantity: 1);

        _patientRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns([lowDebt, highDebt]);
        _planRepo.GetAllWithServiceAsync(Arg.Any<CancellationToken>()).Returns([lowPlan, highPlan]);

        var result = await _handler.Handle(new GetAllPatientsBalanceQuery(), CancellationToken.None);

        result[0].PatientId.Should().Be(highDebt.Id);
        result[1].PatientId.Should().Be(lowDebt.Id);
    }

    private static Patient MakePatient(string fullName)
    {
        var user = User.Create($"u{Guid.NewGuid():N}"[..10], $"{Guid.NewGuid():N}@test.com", "hash", Domain.Enums.UserRole.Patient, null, fullName);
        var patient = Patient.Create(user.Id);
        patient.User = user;
        return patient;
    }

    private static Service MakeService(string name, decimal price)
        => Service.Create(name, price, 30, "Mô tả test");

    private static TreatmentPlan MakePlan(Guid patientId, Service service, decimal unitPrice, int quantity)
    {
        var plan = TreatmentPlan.Create(patientId, Guid.NewGuid(), null, service.Id, unitPrice, quantity);
        typeof(TreatmentPlan).GetProperty(nameof(TreatmentPlan.Service))!.SetValue(plan, service);
        return plan;
    }
}
