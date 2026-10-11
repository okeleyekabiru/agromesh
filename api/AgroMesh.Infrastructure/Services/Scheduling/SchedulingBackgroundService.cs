using AgroMesh.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgroMesh.Infrastructure.Services.Scheduling;

public class SchedulingBackgroundService : BackgroundService
{
    private readonly BackgroundJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SchedulingBackgroundService> _logger;

    public SchedulingBackgroundService(BackgroundJobQueue queue, IServiceScopeFactory scopeFactory, ILogger<SchedulingBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var schedulingService = scope.ServiceProvider.GetRequiredService<IClaudeSchedulingService>();
                await schedulingService.RunSchedulingForBlockAsync(job.FarmId, job.BlockId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background scheduling run failed for block {BlockId}", job.BlockId);
            }
        }
    }
}
