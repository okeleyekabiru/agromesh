namespace AgroMesh.Application.Interfaces;

public interface IBackgroundJobQueue
{
    void EnqueueSchedulingRun(string farmId, string blockId);
}
