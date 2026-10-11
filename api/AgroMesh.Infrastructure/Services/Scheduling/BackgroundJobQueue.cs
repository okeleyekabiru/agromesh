using System.Threading.Channels;
using AgroMesh.Application.Interfaces;

namespace AgroMesh.Infrastructure.Services.Scheduling;

public record SchedulingJob(string FarmId, string BlockId);

public class BackgroundJobQueue : IBackgroundJobQueue
{
    private readonly Channel<SchedulingJob> _channel = Channel.CreateUnbounded<SchedulingJob>();

    public void EnqueueSchedulingRun(string farmId, string blockId) =>
        _channel.Writer.TryWrite(new SchedulingJob(farmId, blockId));

    public IAsyncEnumerable<SchedulingJob> DequeueAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
