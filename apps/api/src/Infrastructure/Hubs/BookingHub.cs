using Microsoft.AspNetCore.SignalR;

namespace DentalClinic.API.Infrastructure.Hubs;

public class BookingHub : Hub
{
    public async Task JoinDateGroup(string date)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"date_{date}");
    }

    public async Task LeaveDateGroup(string date)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"date_{date}");
    }

    public async Task JoinDentistDateGroup(string dentistId, string date)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"dentist_{dentistId}_{date}");
    }

    public async Task LeaveDentistDateGroup(string dentistId, string date)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"dentist_{dentistId}_{date}");
    }
}
