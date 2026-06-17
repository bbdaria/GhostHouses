using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebServer.Data;

namespace WebServer.Services;

public class ExternalSyncWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExternalSyncWorker> _logger;

    public ExternalSyncWorker(IServiceProvider serviceProvider, ILogger<ExternalSyncWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var externalService = scope.ServiceProvider.GetRequiredService<IExternalDataService>();

                var buildingIds = await context.Buildings.Select(b => b.Id).ToListAsync(stoppingToken);
                foreach (var id in buildingIds)
                {
                    await externalService.SnapshotAsync(id, stoppingToken);
                }

                _logger.LogInformation("External data sync completed for {Count} buildings", buildingIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while running external data sync");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}
