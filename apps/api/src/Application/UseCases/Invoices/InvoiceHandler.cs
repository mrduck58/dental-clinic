using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Invoices;

#region DTOs & Requests

/// <summary>Một dòng dịch vụ trên hóa đơn (hoặc gợi ý từ liệu trình).</summary>
public record InvoiceItemDto(string Name, int Quantity, decimal UnitPrice, Guid? TreatmentPlanId = null)
{
    public decimal LineTotal => Quantity * UnitPrice;
}

/// <summary>
/// Liệu trình điều trị đang chờ xuất hóa đơn — gom theo lịch hẹn đã kết thúc điều trị
/// (trạng thái PendingPayment) và chưa có hóa đơn.
/// </summary>
public class BillablePlanDto
{
    public Guid AppointmentId { get; set; }
    public string AppointmentCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? Gender { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public DateTimeOffset AppointmentDate { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public List<InvoiceItemDto> Items { get; set; } = new();
    public decimal SuggestedTotal { get; set; }

    // Khi mục này là "thu phần còn lại" của một hóa đơn đặt cọc
    public Guid? OutstandingInvoiceId { get; set; }
    public string? SourceInvoiceNumber { get; set; }

    // Khi mục này là một đợt thu của liệu trình điều trị
    public Guid? TreatmentPlanId { get; set; }
    public string? PlanName { get; set; }
    public decimal PlanTotal { get; set; }
    public decimal PlanAmountPaid { get; set; }
    public decimal PlanRemaining { get; set; }
}

/// <summary>Công nợ ở cấp liệu trình điều trị (cho tab Công nợ).</summary>
public class OutstandingPlanDto
{
    public Guid TreatmentPlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? Gender { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Hóa đơn đầy đủ trả về cho tab "Chờ thanh toán" và "Lịch sử".</summary>
public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid AppointmentId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? Gender { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public DateTimeOffset AppointmentDate { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentType { get; set; } = string.Empty;   // "Full" | "Deposit"
    public decimal DepositAmount { get; set; }                // Số tiền thu trên hóa đơn này
    public decimal RemainingAmount { get; set; }              // Số tiền còn lại
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaymentDate { get; set; }

    // Công nợ
    public Guid? ParentInvoiceId { get; set; }     // hóa đơn gốc nếu đây là hóa đơn thu phần còn lại
    public bool IsSettled { get; set; }            // hóa đơn đặt cọc đã tất toán
    public bool CollectingRemaining { get; set; }  // đang trong quy trình thu phần còn lại
}

public record IssueInvoiceItemRequest(string Name, int Quantity, decimal UnitPrice, Guid? TreatmentPlanId = null, decimal? AmountCollected = null);

public record IssueInvoiceRequest(
    Guid AppointmentId,
    List<IssueInvoiceItemRequest> Items,
    decimal Discount,
    string PaymentMethod,
    string? PaymentType,
    decimal DepositAmount,
    string? Notes,
    Guid? ParentInvoiceId,
    Guid? TreatmentPlanId);

public record ConfirmPaymentRequest(string? PaymentMethod);

#endregion

public class InvoiceHandler(
    AppDbContext dbContext,
    INotificationService notificationService,
    IUserRepository userRepository)
{
    /// <summary>
    /// Tab "Liệu trình → Hóa đơn": lịch hẹn đã kết thúc điều trị, chưa xuất hóa đơn.
    /// Các dòng dịch vụ được gợi ý từ liệu trình điều trị (Description + EstimatedCost).
    /// </summary>
    public async Task<List<BillablePlanDto>> GetBillablePlansAsync(CancellationToken ct = default)
    {
        // 1) Buổi đã kết thúc điều trị — cho phép xuất NHIỀU hóa đơn/buổi (mỗi dịch vụ một hóa đơn,
        // kiểu thanh toán riêng), nên KHÔNG lọc "buổi chưa có hóa đơn".
        var appointments = await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist).ThenInclude(d => d.User)
            .Include(a => a.Diagnoses)
            .Where(a => a.Status == AppointmentStatus.PendingPayment)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync(ct);

        var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();

        // Tất cả liệu trình (chưa hủy) của các bệnh nhân liên quan — KHÔNG lọc theo trạng thái lâm sàng:
        // một dịch vụ chưa thu đủ tiền vẫn phải xuất hóa đơn dù bác sĩ đã đánh dấu "Hoàn thành".
        var allPlans = patientIds.Count == 0
            ? new List<TreatmentPlan>()
            : await dbContext.TreatmentPlans
                .AsNoTracking()
                .Include(tp => tp.Service)
                .Where(tp => patientIds.Contains(tp.PatientId) && tp.Status != TreatmentPlanStatus.Cancelled)
                .ToListAsync(ct);
        var planIds = allPlans.Select(p => p.Id).ToList();
        var billedByPlan = await GetPlanBilledMapAsync(planIds, ct); // đã gắn vào hóa đơn (tránh xuất trùng)
        var paidByPlan = await GetPlanPaidMapAsync(planIds, ct);     // đã thu (hiển thị)

        // Bản đồ cha-con để gom liệu trình theo ĐÚNG chuỗi tái khám của mỗi buổi.
        var parentById = patientIds.Count == 0
            ? new Dictionary<Guid, Guid?>()
            : (await dbContext.Appointments
                .AsNoTracking()
                .Where(a => patientIds.Contains(a.PatientId))
                .Select(a => new { a.Id, a.FollowUpFromAppointmentId })
                .ToListAsync(ct))
                .ToDictionary(a => a.Id, a => a.FollowUpFromAppointmentId);

        HashSet<Guid> ChainOf(Guid id)
        {
            var chain = new HashSet<Guid>();
            Guid? cursor = id;
            while (cursor is Guid c && chain.Add(c))
                cursor = parentById.TryGetValue(c, out var next) ? next : null;
            return chain;
        }

        // Gán mỗi dịch vụ chưa xuất hóa đơn hết cho buổi PendingPayment MỚI NHẤT trong chuỗi của nó,
        // rồi gom các dịch vụ theo buổi → mỗi buổi MỘT thẻ (nhiều dòng, chọn thanh toán theo từng dòng).
        var byHost = new Dictionary<Guid, List<TreatmentPlan>>();
        foreach (var plan in allPlans.OrderBy(p => p.CreatedAt))
        {
            if (plan.AppointmentId is not Guid planApptId) continue;
            if (plan.TotalCost - billedByPlan.GetValueOrDefault(plan.Id, 0m) <= 0) continue;

            Appointment? host = null;
            foreach (var a in appointments)
                if (a.PatientId == plan.PatientId && ChainOf(a.Id).Contains(planApptId)
                    && (host == null || a.AppointmentDate > host.AppointmentDate))
                    host = a;
            if (host == null) continue;

            if (!byHost.TryGetValue(host.Id, out var list)) byHost[host.Id] = list = new List<TreatmentPlan>();
            list.Add(plan);
        }

        var result = new List<BillablePlanDto>();
        foreach (var a in appointments)
        {
            if (!byHost.TryGetValue(a.Id, out var plans)) continue;
            var items = plans.Select(p =>
            {
                var billed = billedByPlan.GetValueOrDefault(p.Id, 0m);
                var toBill = p.TotalCost - billed;
                return billed <= 0
                    ? new InvoiceItemDto(BuildPlanName(p), p.Quantity, p.UnitPrice, p.Id)
                    : new InvoiceItemDto($"{BuildPlanName(p)} (còn lại)", 1, toBill, p.Id);
            }).ToList();

            result.Add(new BillablePlanDto
            {
                AppointmentId = a.Id,
                AppointmentCode = BuildAppointmentCode(a),
                PatientName = a.Patient.FullName,
                PatientPhone = a.Patient.User?.PhoneNumber,
                Gender = a.Patient.Gender,
                DentistName = a.Dentist.FullName,
                AppointmentDate = a.AppointmentDate,
                Diagnosis = string.Join("; ", a.Diagnoses.Select(d => d.Description)),
                Items = items,
                SuggestedTotal = items.Sum(i => i.LineTotal)
            });
        }

        // 2) Hóa đơn đặt cọc đang được yêu cầu thu phần còn lại (chưa tạo hóa đơn con).
        var remainingParents = await dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
            .Include(i => i.Appointment).ThenInclude(a => a.Dentist).ThenInclude(d => d.User)
            .Where(i => i.CollectingRemaining && !i.IsSettled && i.TotalAmount > i.DepositAmount
                        && !dbContext.Invoices.Any(c => c.ParentInvoiceId == i.Id))
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

        foreach (var parent in remainingParents)
        {
            var a = parent.Appointment;
            var remaining = parent.TotalAmount - parent.DepositAmount;
            result.Add(new BillablePlanDto
            {
                AppointmentId = a.Id,
                AppointmentCode = BuildAppointmentCode(a),
                PatientName = a.Patient.FullName,
                PatientPhone = a.Patient.User?.PhoneNumber,
                Gender = a.Patient.Gender,
                DentistName = a.Dentist.FullName,
                AppointmentDate = a.AppointmentDate,
                Diagnosis = $"Thu phần còn lại của hóa đơn {parent.InvoiceNumber}",
                Items = new List<InvoiceItemDto> { new($"Phần còn lại - HĐ {parent.InvoiceNumber}", 1, remaining) },
                SuggestedTotal = remaining,
                OutstandingInvoiceId = parent.Id,
                SourceInvoiceNumber = parent.InvoiceNumber
            });
        }

        return result;
    }

    /// <summary>Xuất hóa đơn từ liệu trình điều trị của một lịch hẹn (hoặc thu phần còn lại).</summary>
    public async Task<InvoiceDto> IssueAsync(IssueInvoiceRequest request, CancellationToken ct = default)
    {
        // Trường hợp thu một đợt của liệu trình điều trị.
        if (request.TreatmentPlanId is Guid treatmentPlanId)
            return await IssuePlanInstallmentAsync(treatmentPlanId, request, ct);

        // Trường hợp thu phần còn lại của một hóa đơn đặt cọc.
        if (request.ParentInvoiceId is Guid parentId)
            return await IssueRemainingAsync(parentId, request, ct);

        var appointment = await dbContext.Appointments
            .Include(a => a.Invoices)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status != AppointmentStatus.PendingPayment)
            throw new ValidationException("Chỉ có thể xuất hóa đơn cho lịch hẹn đã kết thúc điều trị (chờ thanh toán).");

        if (request.Items == null || request.Items.Count == 0)
            throw new ValidationException("Hóa đơn phải có ít nhất một dịch vụ.");

        // Cho phép nhiều hóa đơn/buổi, nhưng chặn xuất vượt tổng tiền của mỗi liệu trình
        // (tránh xuất trùng dịch vụ đã có hóa đơn trước đó).
        var lineByPlan = request.Items
            .Where(i => i.TreatmentPlanId != null)
            .GroupBy(i => i.TreatmentPlanId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(i => (i.Quantity < 1 ? 1 : i.Quantity) * i.UnitPrice));
        if (lineByPlan.Count > 0)
        {
            var billedMap = await GetPlanBilledMapAsync(lineByPlan.Keys.ToList(), ct);
            var plans = await dbContext.TreatmentPlans
                .Where(tp => lineByPlan.Keys.Contains(tp.Id))
                .Select(tp => new { tp.Id, tp.UnitPrice, tp.Quantity })
                .ToListAsync(ct);
            foreach (var p in plans)
            {
                var total = p.UnitPrice * Math.Max(1, p.Quantity);
                var remainingToBill = total - billedMap.GetValueOrDefault(p.Id, 0m);
                if (lineByPlan[p.Id] > remainingToBill + 1m)
                    throw new ValidationException("Số tiền xuất hóa đơn vượt quá phần chưa xuất của dịch vụ.");
            }
        }

        var paymentMethod = ParsePaymentMethod(request.PaymentMethod);

        // Thu ngay của mỗi dòng: mặc định = thành tiền (thu toàn bộ); nếu đặt cọc thì là số cọc của dòng.
        var invoiceNumber = await GenerateInvoiceNumberAsync(ct);

        var invoice = Invoice.Issue(
            appointment.Id,
            invoiceNumber,
            request.Items.Select(i => (i.Name, i.Quantity, i.UnitPrice, i.TreatmentPlanId, i.AmountCollected)),
            request.Discount,
            paymentMethod,
            request.Notes);

        if (invoice.DepositAmount <= 0)
            throw new ValidationException("Số tiền thu phải lớn hơn 0.");

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(ct);
        await NotifyInvoiceIssuedAsync(invoice, ct);

        return await GetByIdAsync(invoice.Id, ct);
    }

    /// <summary>Tạo hóa đơn thu nốt phần còn lại cho một hóa đơn đặt cọc.</summary>
    private async Task<InvoiceDto> IssueRemainingAsync(Guid parentId, IssueInvoiceRequest request, CancellationToken ct)
    {
        var parent = await dbContext.Invoices
            .FirstOrDefaultAsync(i => i.Id == parentId, ct)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn gốc.");

        if (parent.IsSettled || parent.RemainingAmount <= 0)
            throw new ValidationException("Hóa đơn này đã được thu đủ.");

        var alreadyHasChild = await dbContext.Invoices.AnyAsync(c => c.ParentInvoiceId == parentId, ct);
        if (alreadyHasChild)
            throw new ConflictException("Đã tạo hóa đơn thu phần còn lại cho hóa đơn này.");

        var paymentMethod = ParsePaymentMethod(request.PaymentMethod);
        var invoiceNumber = await GenerateInvoiceNumberAsync(ct);

        var invoice = Invoice.IssueRemaining(
            parent.AppointmentId,
            invoiceNumber,
            parent.Id,
            $"Phần còn lại - HĐ {parent.InvoiceNumber}",
            parent.RemainingAmount,
            paymentMethod,
            request.Notes);

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(ct);
        await NotifyInvoiceIssuedAsync(invoice, ct);

        return await GetByIdAsync(invoice.Id, ct);
    }

    /// <summary>Tạo một đợt thu của liệu trình điều trị (số tiền tùy ý, không vượt công nợ còn lại).</summary>
    private async Task<InvoiceDto> IssuePlanInstallmentAsync(Guid treatmentPlanId, IssueInvoiceRequest request, CancellationToken ct)
    {
        var plan = await dbContext.TreatmentPlans
            .Include(tp => tp.Service)
            .FirstOrDefaultAsync(tp => tp.Id == treatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình.");

        if (plan.Status != TreatmentPlanStatus.InProgress)
            throw new ValidationException("Chỉ có thể thu đợt cho liệu trình đang điều trị.");

        var appointment = await dbContext.Appointments
            .Include(a => a.Invoices)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.PatientId != plan.PatientId)
            throw new ValidationException("Lịch hẹn không thuộc bệnh nhân của liệu trình này.");

        if (appointment.Status != AppointmentStatus.PendingPayment)
            throw new ValidationException("Chỉ có thể thu khi buổi hẹn đã kết thúc điều trị (chờ thanh toán).");

        if (appointment.Invoices.Any())
            throw new ConflictException("Buổi hẹn này đã có đợt thu.");

        var amount = request.Items?.Sum(i => (i.Quantity < 1 ? 1 : i.Quantity) * i.UnitPrice) ?? 0;
        if (amount <= 0)
            throw new ValidationException("Số tiền thu phải lớn hơn 0.");

        var paid = await GetPlanPaidAsync(treatmentPlanId, ct);
        var remaining = plan.TotalCost - paid;
        if (amount > remaining)
            throw new ValidationException($"Số tiền thu vượt quá công nợ còn lại ({remaining:#,##0}đ).");

        var paymentMethod = ParsePaymentMethod(request.PaymentMethod);
        var invoiceNumber = await GenerateInvoiceNumberAsync(ct);

        var invoice = Invoice.IssuePlanInstallment(
            appointment.Id,
            treatmentPlanId,
            invoiceNumber,
            $"Đợt thu - {BuildPlanName(plan)}",
            amount,
            paymentMethod,
            request.Notes);

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(ct);
        await NotifyInvoiceIssuedAsync(invoice, ct);

        return await GetByIdAsync(invoice.Id, ct);
    }

    /// <summary>Tab "Chờ thanh toán": các hóa đơn chưa thanh toán.</summary>
    public async Task<List<InvoiceDto>> GetPendingAsync(CancellationToken ct = default)
    {
        var invoices = await QueryWithDetails()
            .Where(i => i.Status == PaymentStatus.Unpaid)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

        return invoices.Select(ToDto).ToList();
    }

    /// <summary>Hóa đơn chưa thanh toán của một bệnh nhân cụ thể (mobile app — bệnh nhân xem hóa đơn của mình).</summary>
    public async Task<List<InvoiceDto>> GetPendingByPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        var invoices = await QueryWithDetails()
            .Where(i => i.Status == PaymentStatus.Unpaid && i.Appointment.PatientId == patientId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

        return invoices.Select(ToDto).ToList();
    }

    /// <summary>Tab "Lịch sử hóa đơn": các hóa đơn đã thanh toán.</summary>
    public async Task<List<InvoiceDto>> GetHistoryAsync(CancellationToken ct = default)
    {
        var invoices = await QueryWithDetails()
            .Where(i => i.Status == PaymentStatus.Paid)
            .OrderByDescending(i => i.PaymentDate)
            .ToListAsync(ct);

        return invoices.Select(ToDto).ToList();
    }

    /// <summary>Hóa đơn đã thanh toán của một bệnh nhân cụ thể (mobile app — tab "Lịch sử giao dịch").</summary>
    public async Task<List<InvoiceDto>> GetPaidByPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        var invoices = await QueryWithDetails()
            .Where(i => i.Status == PaymentStatus.Paid && i.Appointment.PatientId == patientId)
            .OrderByDescending(i => i.PaymentDate)
            .ToListAsync(ct);

        return invoices.Select(ToDto).ToList();
    }

    /// <summary>
    /// Tab "Công nợ": các hóa đơn chưa thu đủ — số tiền đã thu nhỏ hơn tổng tiền
    /// (hóa đơn đặt cọc còn dư nợ). Không tính hóa đơn đã hoàn tiền.
    /// </summary>
    public async Task<List<InvoiceDto>> GetOutstandingAsync(CancellationToken ct = default)
    {
        // Chỉ tính hóa đơn đặt cọc còn dư nợ, chưa tất toán (không tính hóa đơn con thu phần còn lại).
        var invoices = await QueryWithDetails()
            .Where(i => i.Status != PaymentStatus.Refunded
                        && !i.IsSettled
                        && i.ParentInvoiceId == null
                        && i.DepositAmount < i.TotalAmount)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        return invoices.Select(ToDto).ToList();
    }

    /// <summary>Tab "Công nợ" — phần liệu trình điều trị còn nợ.</summary>
    public async Task<List<OutstandingPlanDto>> GetOutstandingPlansAsync(CancellationToken ct = default)
    {
        var plans = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Patient).ThenInclude(p => p.User)
            .Include(tp => tp.Dentist).ThenInclude(d => d.User)
            .Include(tp => tp.Service)
            .Where(tp => tp.Status == TreatmentPlanStatus.InProgress)
            .OrderByDescending(tp => tp.CreatedAt)
            .ToListAsync(ct);

        var paidMap = await GetPlanPaidMapAsync(plans.Select(p => p.Id).ToList(), ct);

        return plans
            .Select(p =>
            {
                var paid = paidMap.GetValueOrDefault(p.Id, 0m);
                return new OutstandingPlanDto
                {
                    TreatmentPlanId = p.Id,
                    PlanName = BuildPlanName(p),
                    PatientName = p.Patient.FullName,
                    PatientPhone = p.Patient.User?.PhoneNumber,
                    Gender = p.Patient.Gender,
                    DentistName = p.Dentist.FullName,
                    TotalCost = p.TotalCost,
                    AmountPaid = paid,
                    RemainingAmount = Math.Max(0, p.TotalCost - paid),
                    Status = p.Status.ToString(),
                    CreatedAt = p.CreatedAt
                };
            })
            // Chỉ tính là công nợ khi ĐÃ thu một phần mà còn thiếu (trả góp dở dang).
            // Liệu trình chưa thu đồng nào chỉ là "đang điều trị", chưa phải nợ — sẽ xuất
            // hóa đơn ở tab "Liệu trình → Hóa đơn" của buổi tương ứng.
            .Where(dto => dto.AmountPaid > 0 && dto.RemainingAmount > 0)
            .ToList();
    }

    /// <summary>Xác nhận đã thanh toán hóa đơn → hoàn tất lịch hẹn / tất toán công nợ.</summary>
    public async Task<InvoiceDto> ConfirmPaymentAsync(Guid invoiceId, ConfirmPaymentRequest request, CancellationToken ct = default)
    {
        var invoice = await dbContext.Invoices
            .Include(i => i.Appointment)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn.");

        if (invoice.Status == PaymentStatus.Paid)
            throw new ConflictException("Hóa đơn này đã được thanh toán.");

        var paymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod)
            ? invoice.PaymentMethod
            : ParsePaymentMethod(request.PaymentMethod);

        await ApplyPaymentConfirmedAsync(invoice, paymentMethod, ct);
        await dbContext.SaveChangesAsync(ct);

        return await GetByIdAsync(invoice.Id, ct);
    }

    /// <summary>
    /// Áp dụng việc xác nhận thanh toán: đánh dấu Paid, tất toán hóa đơn gốc (nếu là hóa đơn thu phần còn lại),
    /// kiểm tra hoàn tất liệu trình dài hạn, hoàn tất lịch hẹn, và bắn notification cho bệnh nhân + staff.
    /// Dùng chung cho cả xác nhận thủ công (<see cref="ConfirmPaymentAsync"/>) và xác nhận qua webhook cổng
    /// thanh toán (PaymentHandler), để không lặp lại các quy tắc nghiệp vụ ở hai nơi.
    /// Idempotent — không làm gì nếu hóa đơn đã Paid (tránh xử lý trùng khi có race giữa xác nhận tay và webhook).
    /// Không tự gọi SaveChangesAsync — caller quyết định thời điểm commit.
    /// </summary>
    internal async Task ApplyPaymentConfirmedAsync(Invoice invoice, PaymentMethod paymentMethod, CancellationToken ct)
    {
        if (invoice.Status == PaymentStatus.Paid) return;

        invoice.MarkAsPaid(paymentMethod);

        // Đóng mọi giao dịch cổng thanh toán còn Pending của hóa đơn này (nếu có) — hóa đơn vừa được xác nhận
        // thanh toán (thủ công, qua webhook, hay qua đối soát dự phòng — hàm này dùng chung cho cả 3 đường), các
        // giao dịch Pending khác chắc chắn sẽ không bao giờ Paid được nữa (CreatePaymentRequestAsync chặn tạo mới
        // khi hóa đơn đã Paid). Nếu không dọn, dữ liệu sẽ có invoice=Paid nhưng transaction=Pending mãi mãi — gây
        // nhiễu khi tra soát và tốn lệnh gọi PayOS mỗi lượt poll nếu còn client đang mở màn hình hóa đơn đó.
        var pendingTxns = await dbContext.PaymentTransactions
            .Where(t => t.InvoiceId == invoice.Id && t.Status == TransactionStatus.Pending)
            .ToListAsync(ct);
        foreach (var t in pendingTxns)
            t.MarkFailed("Hóa đơn đã được xác nhận thanh toán.", null);

        // Nếu đây là hóa đơn thu phần còn lại → tất toán hóa đơn cọc gốc.
        Invoice? parent = null;
        if (invoice.ParentInvoiceId is Guid parentId)
        {
            parent = await dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == parentId, ct);
            parent?.Settle();
        }

        // Credit công nợ cho từng liệu trình theo SỐ THU CỦA TỪNG DÒNG.
        // - Hóa đơn thường: mỗi dòng credit đúng AmountCollected của nó (toàn bộ hoặc cọc của dòng).
        // - Hóa đơn thu phần còn lại: lấy dòng của hóa đơn cọc gốc, credit nốt phần còn nợ của từng dòng.
        var creditSource = parent ?? invoice;
        var creditRemaining = parent != null; // true → thu nốt phần còn lại của các dòng gốc

        var lines = await dbContext.InvoiceItems
            .Where(it => it.InvoiceId == creditSource.Id && it.TreatmentPlanId != null)
            .Select(it => new
            {
                PlanId = it.TreatmentPlanId!.Value,
                Collected = it.AmountCollected,
                Line = it.Quantity * it.UnitPrice
            })
            .ToListAsync(ct);

        var creditByPlan = new Dictionary<Guid, decimal>();
        foreach (var l in lines)
        {
            var credit = creditRemaining ? Math.Max(0, l.Line - l.Collected) : l.Collected;
            creditByPlan[l.PlanId] = creditByPlan.GetValueOrDefault(l.PlanId, 0m) + credit;
        }
        // Tương thích ngược: hóa đơn "đợt thu" cũ gắn liệu trình ở cấp hóa đơn.
        if ((parent?.TreatmentPlanId ?? invoice.TreatmentPlanId) is Guid headerPlanId)
        {
            var headerCredit = creditRemaining ? (parent?.RemainingAmount ?? 0m) : invoice.DepositAmount;
            creditByPlan[headerPlanId] = creditByPlan.GetValueOrDefault(headerPlanId, 0m) + headerCredit;
        }

        foreach (var (pid, thisCredit) in creditByPlan)
        {
            var plan = await dbContext.TreatmentPlans.FirstOrDefaultAsync(tp => tp.Id == pid, ct);
            if (plan != null && plan.Status == TreatmentPlanStatus.InProgress)
            {
                // GetPlanPaidAsync đọc từ DB (chưa gồm thay đổi lần này) → cộng thêm số vừa thu.
                var paidBefore = await GetPlanPaidAsync(pid, ct);
                if (paidBefore + thisCredit >= plan.TotalCost - 1m) // dung sai làm tròn
                    plan.SetStatus(TreatmentPlanStatus.Completed);
            }
        }

        // Hoàn tất lịch hẹn CHỈ khi mọi dịch vụ trong chuỗi đã xuất hóa đơn hết
        // (cho phép còn dịch vụ khác của buổi chưa xuất → giữ buổi ở "chờ thanh toán").
        if (invoice.Appointment.Status == AppointmentStatus.PendingPayment)
        {
            var chain = await GetChainAsync(invoice.AppointmentId, ct);
            var chainPlans = await dbContext.TreatmentPlans
                .Where(tp => tp.AppointmentId != null && chain.Contains(tp.AppointmentId.Value)
                             && tp.Status != TreatmentPlanStatus.Cancelled)
                .Select(tp => new { tp.Id, tp.UnitPrice, tp.Quantity })
                .ToListAsync(ct);
            var billedMap = await GetPlanBilledMapAsync(chainPlans.Select(p => p.Id).ToList(), ct);
            var anyUnbilled = chainPlans.Any(p =>
                (p.UnitPrice * Math.Max(1, p.Quantity)) - billedMap.GetValueOrDefault(p.Id, 0m) > 1m);
            if (!anyUnbilled)
                invoice.Appointment.Complete();
        }

        await NotifyPaymentConfirmedAsync(invoice, paymentMethod, ct);
    }

    /// <summary>
    /// Bắt đầu thu phần còn lại của một hóa đơn đặt cọc — đưa vào danh sách "Liệu trình → Hóa đơn".
    /// </summary>
    public async Task<InvoiceDto> CollectRemainingAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await dbContext.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn.");

        if (invoice.IsSettled || invoice.RemainingAmount <= 0)
            throw new ValidationException("Hóa đơn này không còn công nợ để thu.");

        var alreadyHasChild = await dbContext.Invoices.AnyAsync(c => c.ParentInvoiceId == invoiceId, ct);
        if (alreadyHasChild)
            throw new ConflictException("Đã tạo hóa đơn thu phần còn lại cho hóa đơn này.");

        invoice.StartCollectingRemaining();
        await dbContext.SaveChangesAsync(ct);

        return await GetByIdAsync(invoice.Id, ct);
    }

    // ── Notifications ────────────────────────────────────────────────────────

    /// <summary>Báo cho bệnh nhân (nếu có tài khoản liên kết) khi có hóa đơn mới cần thanh toán.</summary>
    private async Task NotifyInvoiceIssuedAsync(Invoice invoice, CancellationToken ct)
    {
        var patient = await dbContext.Appointments
            .Where(a => a.Id == invoice.AppointmentId)
            .Select(a => a.Patient)
            .FirstOrDefaultAsync(ct);

        if (patient?.UserId is not Guid patientUserId) return;

        await notificationService.CreateAsync(new CreateNotificationRequest(
            UserId: patientUserId,
            Type: NotificationType.Invoice,
            Priority: NotificationPriority.Medium,
            Title: "Hóa đơn mới",
            Body: $"Bạn có hóa đơn {invoice.InvoiceNumber} cần thanh toán ({invoice.DepositAmount:#,##0}đ).",
            RelatedEntityType: "Invoice",
            RelatedEntityId: invoice.Id.ToString()), ct);
    }

    /// <summary>Báo cho bệnh nhân (nếu có tài khoản) và toàn bộ Staff khi một hóa đơn được xác nhận đã thanh toán.</summary>
    private async Task NotifyPaymentConfirmedAsync(Invoice invoice, PaymentMethod paymentMethod, CancellationToken ct)
    {
        var patient = await dbContext.Appointments
            .Where(a => a.Id == invoice.AppointmentId)
            .Select(a => a.Patient)
            .FirstOrDefaultAsync(ct);

        if (patient?.UserId is Guid patientUserId)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: patientUserId,
                Type: NotificationType.Invoice,
                Priority: NotificationPriority.Medium,
                Title: "Thanh toán thành công",
                Body: $"Hóa đơn {invoice.InvoiceNumber} đã được thanh toán ({invoice.DepositAmount:#,##0}đ) qua {DescribePaymentMethod(paymentMethod)}.",
                RelatedEntityType: "Invoice",
                RelatedEntityId: invoice.Id.ToString()), ct);
        }

        var staffIds = await userRepository.GetUserIdsByRoleAsync("Staff", ct);
        var staffTemplate = new CreateNotificationRequest(
            UserId: Guid.Empty,
            Type: NotificationType.Invoice,
            Priority: NotificationPriority.Low,
            Title: "Đã nhận thanh toán",
            Body: $"Hóa đơn {invoice.InvoiceNumber} đã được thanh toán qua {DescribePaymentMethod(paymentMethod)}.",
            RelatedEntityType: "Invoice",
            RelatedEntityId: invoice.Id.ToString());
        await notificationService.CreateForMultipleUsersAsync(staffIds, staffTemplate, ct);
    }

    private static string DescribePaymentMethod(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "tiền mặt",
        PaymentMethod.BankTransfer => "chuyển khoản",
        PaymentMethod.OnlinePayment => "thanh toán online",
        _ => method.ToString()
    };

    // ── Helpers ──────────────────────────────────────────────────────────────

    internal async Task<InvoiceDto> GetByIdAsync(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await QueryWithDetails().FirstAsync(i => i.Id == invoiceId, ct);
        return ToDto(invoice);
    }

    private IQueryable<Invoice> QueryWithDetails() =>
        dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.Items)
            .Include(i => i.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
            .Include(i => i.Appointment).ThenInclude(a => a.Dentist).ThenInclude(d => d.User);

    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken ct)
    {
        var count = await dbContext.Invoices.CountAsync(ct);
        return $"INV{count + 1:D3}";
    }

    /// <summary>Tổng số tiền đã thu (Paid) của một liệu trình.</summary>
    private async Task<decimal> GetPlanPaidAsync(Guid treatmentPlanId, CancellationToken ct) =>
        (await GetPlanPaidMapAsync(new List<Guid> { treatmentPlanId }, ct)).GetValueOrDefault(treatmentPlanId, 0m);

    /// <summary>
    /// Map số tiền ĐÃ THU theo từng liệu trình. Mỗi dòng hóa đơn đã Paid credit đúng số thu của dòng
    /// (AmountCollected); nếu hóa đơn cọc đã tất toán thì credit trọn thành tiền dòng.
    /// Cũng gộp các hóa đơn "đợt thu" cũ (gắn liệu trình ở cấp hóa đơn).
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> GetPlanPaidMapAsync(List<Guid> planIds, CancellationToken ct)
    {
        var map = new Dictionary<Guid, decimal>();
        if (planIds.Count == 0) return map;

        // Mô hình mới: dòng hóa đơn gắn TreatmentPlanId, credit theo số thu của từng dòng.
        var lineRows = await dbContext.InvoiceItems
            .Where(it => it.TreatmentPlanId != null && planIds.Contains(it.TreatmentPlanId.Value)
                         && it.Invoice.Status == PaymentStatus.Paid)
            .Select(it => new
            {
                PlanId = it.TreatmentPlanId!.Value,
                Line = it.Quantity * it.UnitPrice,
                it.AmountCollected,
                it.Invoice.IsSettled
            })
            .ToListAsync(ct);
        foreach (var r in lineRows)
        {
            var credit = r.IsSettled ? r.Line : r.AmountCollected;
            map[r.PlanId] = map.GetValueOrDefault(r.PlanId, 0m) + credit;
        }

        // Mô hình cũ: hóa đơn "đợt thu" gắn liệu trình ở cấp hóa đơn.
        var headerRows = await dbContext.Invoices
            .Where(i => i.TreatmentPlanId != null && planIds.Contains(i.TreatmentPlanId.Value)
                        && i.Status == PaymentStatus.Paid)
            .GroupBy(i => i.TreatmentPlanId!.Value)
            .Select(g => new { PlanId = g.Key, Sum = g.Sum(x => x.DepositAmount) })
            .ToListAsync(ct);
        foreach (var h in headerRows)
            map[h.PlanId] = map.GetValueOrDefault(h.PlanId, 0m) + h.Sum;

        return map;
    }

    /// <summary>
    /// Map số tiền ĐÃ GẮN vào hóa đơn (chưa hủy) theo từng liệu trình — để không xuất hóa đơn trùng.
    /// Gồm dòng hóa đơn gắn liệu trình (mô hình mới) và hóa đơn "đợt thu" cũ (gắn ở cấp hóa đơn).
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> GetPlanBilledMapAsync(List<Guid> planIds, CancellationToken ct)
    {
        var map = new Dictionary<Guid, decimal>();
        if (planIds.Count == 0) return map;

        var byLine = await dbContext.InvoiceItems
            .Where(it => it.TreatmentPlanId != null && planIds.Contains(it.TreatmentPlanId.Value)
                         && it.Invoice.Status != PaymentStatus.Refunded)
            .GroupBy(it => it.TreatmentPlanId!.Value)
            .Select(g => new { PlanId = g.Key, Sum = g.Sum(it => it.Quantity * it.UnitPrice) })
            .ToListAsync(ct);
        foreach (var r in byLine)
            map[r.PlanId] = map.GetValueOrDefault(r.PlanId, 0m) + r.Sum;

        var byHeader = await dbContext.Invoices
            .Where(i => i.TreatmentPlanId != null && planIds.Contains(i.TreatmentPlanId.Value)
                        && i.Status != PaymentStatus.Refunded)
            .GroupBy(i => i.TreatmentPlanId!.Value)
            .Select(g => new { PlanId = g.Key, Sum = g.Sum(x => x.TotalAmount) })
            .ToListAsync(ct);
        foreach (var h in byHeader)
            map[h.PlanId] = map.GetValueOrDefault(h.PlanId, 0m) + h.Sum;

        return map;
    }

    /// <summary>Chuỗi tái khám của một buổi hẹn: chính nó + các buổi gốc phía trên (đi ngược FollowUpFromAppointmentId).</summary>
    private async Task<HashSet<Guid>> GetChainAsync(Guid appointmentId, CancellationToken ct)
    {
        var chain = new HashSet<Guid>();
        Guid? cursor = appointmentId;
        while (cursor is Guid c && chain.Add(c))
            cursor = await dbContext.Appointments
                .Where(a => a.Id == c)
                .Select(a => a.FollowUpFromAppointmentId)
                .FirstOrDefaultAsync(ct);
        return chain;
    }

    /// <summary>Tên hiển thị của một liệu trình: tên dịch vụ + răng điều trị (nếu có).</summary>
    private static string BuildPlanName(TreatmentPlan tp) =>
        string.IsNullOrWhiteSpace(tp.Teeth) ? tp.Service.Name : $"{tp.Service.Name} - Răng {tp.Teeth}";

    private static InvoiceDto ToDto(Invoice invoice)
    {
        var appointment = invoice.Appointment;
        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            AppointmentId = invoice.AppointmentId,
            PatientName = appointment.Patient.FullName,
            PatientPhone = appointment.Patient.User?.PhoneNumber,
            Gender = appointment.Patient.Gender,
            DentistName = appointment.Dentist.FullName,
            AppointmentDate = appointment.AppointmentDate,
            Items = invoice.Items
                .Select(i => new InvoiceItemDto(i.Name, i.Quantity, i.UnitPrice, i.TreatmentPlanId))
                .ToList(),
            Subtotal = invoice.Subtotal,
            Discount = invoice.Discount,
            TotalAmount = invoice.TotalAmount,
            PaymentType = invoice.PaymentType.ToString(),
            DepositAmount = invoice.DepositAmount,
            RemainingAmount = invoice.RemainingAmount,
            PaymentMethod = invoice.PaymentMethod.ToString(),
            Status = invoice.Status.ToString(),
            Notes = invoice.Notes,
            CreatedAt = invoice.CreatedAt,
            PaymentDate = invoice.PaymentDate,
            ParentInvoiceId = invoice.ParentInvoiceId,
            IsSettled = invoice.IsSettled,
            CollectingRemaining = invoice.CollectingRemaining
        };
    }

    private static string BuildAppointmentCode(Appointment a) =>
        $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString("N")[..6].ToUpper()}";

    private static PaymentMethod ParsePaymentMethod(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "cash" or "tienmat" => PaymentMethod.Cash,
        "transfer" or "banktransfer" or "bank_transfer" => PaymentMethod.BankTransfer,
        "app" or "online" or "onlinepayment" or "online_payment" => PaymentMethod.OnlinePayment,
        _ when Enum.TryParse<PaymentMethod>(value, ignoreCase: true, out var m) => m,
        _ => throw new ValidationException("Phương thức thanh toán không hợp lệ.")
    };

    private static PaymentType ParsePaymentType(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" or "full" or "toanbo" => PaymentType.Full,
        "deposit" or "datcoc" => PaymentType.Deposit,
        _ when Enum.TryParse<PaymentType>(value, ignoreCase: true, out var t) => t,
        _ => throw new ValidationException("Loại thanh toán không hợp lệ.")
    };
}
