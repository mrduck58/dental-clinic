using DentalClinic.API.Application.UseCases.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = "Staff,Admin,Owner")]
public class InvoicesController(InvoiceHandler invoiceHandler) : ControllerBase
{
    /// <summary>GET api/invoices/billable-plans — Liệu trình điều trị chờ xuất hóa đơn (lịch hẹn đã kết thúc điều trị).</summary>
    [HttpGet("billable-plans")]
    public async Task<IActionResult> GetBillablePlans(CancellationToken cancellationToken)
    {
        var result = await invoiceHandler.GetBillablePlansAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/invoices — Xuất hóa đơn từ liệu trình điều trị.</summary>
    [HttpPost]
    public async Task<IActionResult> IssueInvoice(
        [FromBody] IssueInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await invoiceHandler.IssueAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPending), new { }, result);
    }

    /// <summary>GET api/invoices/pending — Hóa đơn chờ thanh toán.</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        var result = await invoiceHandler.GetPendingAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/invoices/history — Lịch sử hóa đơn đã thanh toán.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        var result = await invoiceHandler.GetHistoryAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/invoices/outstanding — Hóa đơn chưa thu đủ (còn công nợ).</summary>
    [HttpGet("outstanding")]
    public async Task<IActionResult> GetOutstanding(CancellationToken cancellationToken)
    {
        var result = await invoiceHandler.GetOutstandingAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/invoices/outstanding-courses — Liệu trình dài hạn còn công nợ.</summary>
    [HttpGet("outstanding-courses")]
    public async Task<IActionResult> GetOutstandingCourses(CancellationToken cancellationToken)
    {
        var result = await invoiceHandler.GetOutstandingCoursesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/invoices/{id}/pay — Xác nhận đã thanh toán, hoàn tất lịch hẹn.</summary>
    [HttpPut("{id}/pay")]
    public async Task<IActionResult> ConfirmPayment(
        Guid id,
        [FromBody] ConfirmPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await invoiceHandler.ConfirmPaymentAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/invoices/{id}/collect-remaining — Đưa hóa đơn đặt cọc vào danh sách thu phần còn lại.</summary>
    [HttpPut("{id}/collect-remaining")]
    public async Task<IActionResult> CollectRemaining(Guid id, CancellationToken cancellationToken)
    {
        var result = await invoiceHandler.CollectRemainingAsync(id, cancellationToken);
        return Ok(result);
    }
}
