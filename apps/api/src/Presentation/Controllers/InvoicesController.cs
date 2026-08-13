using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Application.UseCases.Invoices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = "Staff,Admin,Owner")]
public class InvoicesController(ISender sender) : ControllerBase
{
    /// <summary>GET api/invoices/billable-plans — Liệu trình điều trị chờ xuất hóa đơn (lịch hẹn đã kết thúc điều trị).</summary>
    [HttpGet("billable-plans")]
    public async Task<IActionResult> GetBillablePlans(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBillablePlansQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/invoices — Xuất hóa đơn từ liệu trình điều trị.</summary>
    [HttpPost]
    public async Task<IActionResult> IssueInvoice(
        [FromBody] IssueInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPending), new { }, result);
    }

    /// <summary>GET api/invoices/pending — Hóa đơn chờ thanh toán.</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPendingInvoicesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/invoices/history — Lịch sử hóa đơn đã thanh toán.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInvoiceHistoryQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/invoices/outstanding — Hóa đơn chưa thu đủ (còn công nợ).</summary>
    [HttpGet("outstanding")]
    public async Task<IActionResult> GetOutstanding(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOutstandingInvoicesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/invoices/outstanding-plans — Liệu trình điều trị còn công nợ.</summary>
    [HttpGet("outstanding-plans")]
    public async Task<IActionResult> GetOutstandingPlans(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOutstandingPlansQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/invoices/{id}/pay — Xác nhận đã thanh toán, hoàn tất lịch hẹn.</summary>
    [HttpPut("{id}/pay")]
    public async Task<IActionResult> ConfirmPayment(
        Guid id,
        [FromBody] ConfirmPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmInvoicePaymentCommand(id, request.PaymentMethod), cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/invoices/{id}/collect-remaining — Đưa hóa đơn đặt cọc vào danh sách thu phần còn lại.</summary>
    [HttpPut("{id}/collect-remaining")]
    public async Task<IActionResult> CollectRemaining(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CollectRemainingInvoiceCommand(id), cancellationToken);
        return Ok(result);
    }
}
