using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DentalClinic.API.Infrastructure.Services;

public class SignalRSlotNotifier(IHubContext<BookingHub> hubContext) : ISlotNotifier
{
    public async Task NotifySlotHeldAsync(
        Guid dentistId,
        DateOnly date,
        string timeSlot,
        Guid heldByPatientId,
        DateTimeOffset expiresAt,
        CancellationToken ct = default)
    {
        var vnDate = date.ToString("yyyy-MM-dd");
        var payload = new
        {
            dentistId = dentistId.ToString(),
            date = vnDate,
            timeSlot,
            heldByPatientId = heldByPatientId.ToString(),
            expiresAt
        };

        await hubContext.Clients.Group($"date_{vnDate}").SendAsync("SlotHeld", payload, ct);
        await hubContext.Clients.Group($"dentist_{dentistId}_{vnDate}").SendAsync("SlotHeld", payload, ct);
    }

    public async Task NotifySlotReleasedAsync(
        Guid dentistId,
        DateOnly date,
        string timeSlot,
        CancellationToken ct = default)
    {
        var vnDate = date.ToString("yyyy-MM-dd");
        var payload = new
        {
            dentistId = dentistId.ToString(),
            date = vnDate,
            timeSlot
        };

        await hubContext.Clients.Group($"date_{vnDate}").SendAsync("SlotReleased", payload, ct);
        await hubContext.Clients.Group($"dentist_{dentistId}_{vnDate}").SendAsync("SlotReleased", payload, ct);
    }

    public async Task NotifySlotBookedAsync(
        Guid dentistId,
        DateOnly date,
        string timeSlot,
        CancellationToken ct = default)
    {
        var vnDate = date.ToString("yyyy-MM-dd");
        var payload = new
        {
            dentistId = dentistId.ToString(),
            date = vnDate,
            timeSlot
        };

        await hubContext.Clients.Group($"date_{vnDate}").SendAsync("SlotBooked", payload, ct);
        await hubContext.Clients.Group($"dentist_{dentistId}_{vnDate}").SendAsync("SlotBooked", payload, ct);
    }
}
