using DentalClinic.API.Application.UseCases.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

/// <summary>Yêu cầu vật tư do BÁC SĨ tạo từ buổi khám (staff xem/xử lý ở InventoryController).</summary>
[ApiController]
[Route("api/inventory/material-requests")]
[Authorize(Roles = "Dentist")]
public class MaterialRequestsController(ISender sender) : ControllerBase
{
    /// <summary>POST api/inventory/material-requests — Bác sĩ gửi yêu cầu vật tư.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMaterialRequestRequest request, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return Ok(result);
    }

    /// <summary>GET api/inventory/material-requests/by-patient?patientId=&amp;name= — Yêu cầu vật tư của một bệnh nhân.</summary>
    [HttpGet("by-patient")]
    public async Task<IActionResult> ByPatient([FromQuery] Guid patientId, [FromQuery] string? name, CancellationToken ct)
    {
        var result = await sender.Send(new GetMaterialRequestsQuery(Status: null, PatientId: patientId, PatientName: name), ct);
        return Ok(result);
    }
}
