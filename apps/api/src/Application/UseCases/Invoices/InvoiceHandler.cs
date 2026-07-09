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
public record InvoiceItemDto(string Name, int Quantity, decimal UnitPrice)
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

    // Khi mục này là một đợt thu của liệu trình dài hạn
    public Guid? CourseId { get; set; }
    public string? CourseName { get; set; }
    public decimal CourseTotal { get; set; }
    public decimal CourseAmountPaid { get; set; }
    public decimal CourseRemaining { get; set; }
}

/// <summary>Công nợ ở cấp liệu trình dài hạn (cho tab Công nợ).</summary>
public class OutstandingCourseDto
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
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

public record IssueInvoiceItemRequest(string Name, int Quantity, decimal UnitPrice);

public record IssueInvoiceRequest(
    Guid AppointmentId,
    List<IssueInvoiceItemRequest> Items,
    decimal Discount,
    string PaymentMethod,
    string? PaymentType,
    decimal DepositAmount,
    string? Notes,
    Guid? ParentInvoiceId,
    Guid? CourseId);

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
        // 1) Lịch hẹn đã kết thúc điều trị, chưa có hóa đơn cho buổi này.
        var appointments = await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist)
            .Include(a => a.Diagnoses)
            .Include(a => a.TreatmentPlans)
            .Include(a => a.Course)
            .Where(a => a.Status == AppointmentStatus.PendingPayment && !a.Invoices.Any())
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync(ct);

        // Số đã thu theo từng liệu trình (cho các buổi thuộc liệu trình dài hạn).
        var courseIds = appointments.Where(a => a.CourseId != null).Select(a => a.CourseId!.Value).Distinct().ToList();
        var paidByCourse = await GetCoursePaidMapAsync(courseIds, ct);

        var result = new List<BillablePlanDto>();
        foreach (var a in appointments)
        {
            if (a.Course is { Status: TreatmentCourseStatus.Active } course)
            {
                // Buổi thuộc liệu trình dài hạn → thu một đợt (nhập số tiền).
                var paid = paidByCourse.GetValueOrDefault(course.Id, 0m);
                var remaining = Math.Max(0, course.TotalCost - paid);
                if (remaining <= 0) continue;

                result.Add(new BillablePlanDto
                {
                    AppointmentId = a.Id,
                    AppointmentCode = BuildAppointmentCode(a),
                    PatientName = a.Patient.FullName,
                    PatientPhone = a.Patient.User?.PhoneNumber,
                    Gender = a.Patient.Gender,
                    DentistName = a.Dentist.FullName,
                    AppointmentDate = a.AppointmentDate,
                    Diagnosis = $"Liệu trình dài hạn: {course.Name}",
                    Items = new List<InvoiceItemDto> { new($"Đợt thu - {course.Name}", 1, remaining) },
                    SuggestedTotal = remaining,
                    CourseId = course.Id,
                    CourseName = course.Name,
                    CourseTotal = course.TotalCost,
                    CourseAmountPaid = paid,
                    CourseRemaining = remaining
                });
            }
            else
            {
                var items = a.TreatmentPlans
                    .OrderBy(tp => tp.CreatedAt)
                    .Select(tp => new InvoiceItemDto(tp.Description, 1, tp.EstimatedCost ?? 0))
                    .ToList();

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
        }

        // 2) Hóa đơn đặt cọc đang được yêu cầu thu phần còn lại (chưa tạo hóa đơn con).
        var remainingParents = await dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
            .Include(i => i.Appointment).ThenInclude(a => a.Dentist)
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
        // Trường hợp thu một đợt của liệu trình dài hạn.
        if (request.CourseId is Guid courseId)
            return await IssueCourseInstallmentAsync(courseId, request, ct);

        // Trường hợp thu phần còn lại của một hóa đơn đặt cọc.
        if (request.ParentInvoiceId is Guid parentId)
            return await IssueRemainingAsync(parentId, request, ct);

        var appointment = await dbContext.Appointments
            .Include(a => a.Invoices)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status != AppointmentStatus.PendingPayment)
            throw new ValidationException("Chỉ có thể xuất hóa đơn cho lịch hẹn đã kết thúc điều trị (chờ thanh toán).");

        if (appointment.Invoices.Any())
            throw new ConflictException("Lịch hẹn này đã có hóa đơn.");

        if (request.Items == null || request.Items.Count == 0)
            throw new ValidationException("Hóa đơn phải có ít nhất một dịch vụ.");

        var paymentMethod = ParsePaymentMethod(request.PaymentMethod);
        var paymentType = ParsePaymentType(request.PaymentType);

        var subtotal = request.Items.Sum(i => (i.Quantity < 1 ? 1 : i.Quantity) * i.UnitPrice);
        var total = Math.Max(0, subtotal - (request.Discount < 0 ? 0 : request.Discount));

        if (paymentType == PaymentType.Deposit && (request.DepositAmount <= 0 || request.DepositAmount > total))
            throw new ValidationException("Số tiền đặt cọc phải lớn hơn 0 và không vượt quá tổng tiền.");

        var invoiceNumber = await GenerateInvoiceNumberAsync(ct);

        var invoice = Invoice.Issue(
            appointment.Id,
            invoiceNumber,
            request.Items.Select(i => (i.Name, i.Quantity, i.UnitPrice)),
            request.Discount,
            paymentMethod,
            paymentType,
            request.DepositAmount,
            request.Notes);

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

    /// <summary>Tạo một đợt thu của liệu trình dài hạn (số tiền tùy ý, không vượt công nợ còn lại).</summary>
    private async Task<InvoiceDto> IssueCourseInstallmentAsync(Guid courseId, IssueInvoiceRequest request, CancellationToken ct)
    {
        var course = await dbContext.TreatmentCourses
            .FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình.");

        if (course.Status != TreatmentCourseStatus.Active)
            throw new ValidationException("Liệu trình đã kết thúc, không thể thu thêm.");

        var appointment = await dbContext.Appointments
            .Include(a => a.Invoices)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.CourseId != courseId)
            throw new ValidationException("Lịch hẹn không thuộc liệu trình này.");

        if (appointment.Status != AppointmentStatus.PendingPayment)
            throw new ValidationException("Chỉ có thể thu khi buổi hẹn đã kết thúc điều trị (chờ thanh toán).");

        if (appointment.Invoices.Any())
            throw new ConflictException("Buổi hẹn này đã có đợt thu.");

        var amount = request.Items?.Sum(i => (i.Quantity < 1 ? 1 : i.Quantity) * i.UnitPrice) ?? 0;
        if (amount <= 0)
            throw new ValidationException("Số tiền thu phải lớn hơn 0.");

        var paid = await GetCoursePaidAsync(courseId, ct);
        var remaining = course.TotalCost - paid;
        if (amount > remaining)
            throw new ValidationException($"Số tiền thu vượt quá công nợ còn lại ({remaining:#,##0}đ).");

        var paymentMethod = ParsePaymentMethod(request.PaymentMethod);
        var invoiceNumber = await GenerateInvoiceNumberAsync(ct);

        var invoice = Invoice.IssueCourseInstallment(
            appointment.Id,
            courseId,
            invoiceNumber,
            $"Đợt thu - {course.Name}",
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

    /// <summary>Tab "Công nợ" — phần liệu trình dài hạn còn nợ.</summary>
    public async Task<List<OutstandingCourseDto>> GetOutstandingCoursesAsync(CancellationToken ct = default)
    {
        var courses = await dbContext.TreatmentCourses
            .AsNoTracking()
            .Include(c => c.Patient).ThenInclude(p => p.User)
            .Include(c => c.Dentist)
            .Where(c => c.Status == TreatmentCourseStatus.Active)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        var paidMap = await GetCoursePaidMapAsync(courses.Select(c => c.Id).ToList(), ct);

        return courses
            .Select(c =>
            {
                var paid = paidMap.GetValueOrDefault(c.Id, 0m);
                return new OutstandingCourseDto
                {
                    CourseId = c.Id,
                    CourseName = c.Name,
                    PatientName = c.Patient.FullName,
                    PatientPhone = c.Patient.User?.PhoneNumber,
                    Gender = c.Patient.Gender,
                    DentistName = c.Dentist.FullName,
                    TotalCost = c.TotalCost,
                    AmountPaid = paid,
                    RemainingAmount = Math.Max(0, c.TotalCost - paid),
                    Status = c.Status.ToString(),
                    CreatedAt = c.CreatedAt
                };
            })
            .Where(dto => dto.RemainingAmount > 0)
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

        // Nếu đây là hóa đơn thu phần còn lại → tất toán hóa đơn gốc.
        if (invoice.ParentInvoiceId is Guid parentId)
        {
            var parent = await dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == parentId, ct);
            parent?.Settle();
        }

        // Nếu đây là đợt thu của liệu trình dài hạn → kiểm tra đã thu đủ chưa.
        if (invoice.CourseId is Guid courseId)
        {
            var course = await dbContext.TreatmentCourses.FirstOrDefaultAsync(c => c.Id == courseId, ct);
            if (course != null && course.Status == TreatmentCourseStatus.Active)
            {
                // GetCoursePaidAsync đọc từ DB (chưa gồm hóa đơn hiện tại) → cộng thêm số của đợt này.
                var paidBefore = await GetCoursePaidAsync(courseId, ct);
                if (paidBefore + invoice.TotalAmount >= course.TotalCost)
                    course.Complete();
            }
        }

        // Hoàn tất lịch hẹn sau khi thanh toán xong.
        if (invoice.Appointment.Status == AppointmentStatus.PendingPayment)
            invoice.Appointment.Complete();

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
            .Include(i => i.Appointment).ThenInclude(a => a.Dentist);

    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken ct)
    {
        var count = await dbContext.Invoices.CountAsync(ct);
        return $"INV{count + 1:D3}";
    }

    /// <summary>Tổng số tiền đã thu (Paid) của một liệu trình.</summary>
    private async Task<decimal> GetCoursePaidAsync(Guid courseId, CancellationToken ct) =>
        await dbContext.Invoices
            .Where(i => i.CourseId == courseId && i.Status == PaymentStatus.Paid)
            .SumAsync(i => (decimal?)i.TotalAmount, ct) ?? 0m;

    /// <summary>Map số tiền đã thu theo từng liệu trình.</summary>
    private async Task<Dictionary<Guid, decimal>> GetCoursePaidMapAsync(List<Guid> courseIds, CancellationToken ct)
    {
        if (courseIds.Count == 0) return new Dictionary<Guid, decimal>();
        var rows = await dbContext.Invoices
            .Where(i => i.CourseId != null && courseIds.Contains(i.CourseId.Value) && i.Status == PaymentStatus.Paid)
            .GroupBy(i => i.CourseId!.Value)
            .Select(g => new { CourseId = g.Key, Paid = g.Sum(x => x.TotalAmount) })
            .ToListAsync(ct);
        return rows.ToDictionary(x => x.CourseId, x => x.Paid);
    }

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
                .Select(i => new InvoiceItemDto(i.Name, i.Quantity, i.UnitPrice))
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
