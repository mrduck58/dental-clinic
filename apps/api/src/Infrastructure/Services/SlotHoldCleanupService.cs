using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Infrastructure.Services;

public class SlotHoldCleanupService(
    IServiceScopeFactory scopeFactory,
    IHubContext<BookingHub> hubContext,
    ILogger<SlotHoldCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                using var scope = scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ISlotHoldRepository>();
                var expiredHolds = await repo.GetExpiredActiveHoldsAsync(now, stoppingToken);

                foreach (var hold in expiredHolds)
                {
                    hold.MarkExpired();
                    await repo.UpdateAsync(hold, stoppingToken);

                    var vnDate = hold.AppointmentDate.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-dd");
                    await hubContext.Clients.Group($"date_{vnDate}").SendAsync("SlotReleased", new
                    {
                        dentistId = hold.DentistId.ToString(),
                        date = vnDate,
                        timeSlot = hold.TimeSlot
                    }, stoppingToken);

                    await hubContext.Clients.Group($"dentist_{hold.DentistId}_{vnDate}").SendAsync("SlotReleased", new
                    {
                        dentistId = hold.DentistId.ToString(),
                        date = vnDate,
                        timeSlot = hold.TimeSlot
                    }, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during slot hold cleanup");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
