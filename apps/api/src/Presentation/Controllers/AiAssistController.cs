using DentalClinic.API.Application.UseCases.AiAssist;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

/// <summary>
/// Bounded context AiAssist — các endpoint gợi ý/tóm tắt bằng AI cho bác sĩ trong lúc khám.
/// Route ghi TUYỆT ĐỐI, y hệt path cũ trong AppointmentsController.
/// </summary>
[ApiController]
public class AiAssistController(ISender sender) : ControllerBase
{
    /// <summary>GET api/appointments/{id}/ai-summary?force=true — Tóm tắt lịch sử khám của bệnh nhân bằng
    /// AI (Staff/Admin/Dentist). Mặc định trả cache nếu chưa có lịch khám mới; force=true bắt tạo lại.</summary>
    [HttpGet("api/appointments/{id}/ai-summary")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> GetPatientAiSummary(
        Guid id, [FromQuery] bool force, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SummarizePatientHistoryQuery(id, force), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/{id}/treatment-suggestion — Gợi ý hướng điều trị bằng AI dựa trên
    /// phiếu khám đã lưu của buổi khám này + lịch sử khám trước đây (Dentist). Cần lưu phiếu khám trước.</summary>
    [HttpGet("api/appointments/{id}/treatment-suggestion")]
    [Authorize(Roles = "Dentist")]
    public async Task<IActionResult> GetTreatmentSuggestion(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SuggestTreatmentQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/{id}/prescription-suggestion — Gợi ý đơn thuốc bằng AI dựa trên
    /// chẩn đoán + liệu trình điều trị của buổi khám này (Dentist). Cần lưu phiếu khám trước.</summary>
    [HttpGet("api/appointments/{id}/prescription-suggestion")]
    [Authorize(Roles = "Dentist")]
    public async Task<IActionResult> GetPrescriptionSuggestion(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SuggestPrescriptionQuery(id), cancellationToken);
        return Ok(result);
    }
}
