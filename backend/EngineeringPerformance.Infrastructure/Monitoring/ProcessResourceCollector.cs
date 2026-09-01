using System.Diagnostics;
using EngineeringPerformance.Application.Monitoring;

namespace EngineeringPerformance.Infrastructure.Monitoring;

public sealed class ProcessResourceCollector : IDisposable
{
    private readonly DotNetRuntimeCounters _counters = new();
    private readonly Process _process = Process.GetCurrentProcess();
    private TimeSpan _previousCpu = TimeSpan.Zero;
    private DateTime _previousUtc = DateTime.MinValue;
    private long _previousAllocatedBytes;
    private bool _hasBaseline;

    internal ProcessSnapshot Capture(DateTime nowUtc)
    {
        _process.Refresh();
        var cpu = _process.TotalProcessorTime;
        var allocated = GC.GetTotalAllocatedBytes(precise: false);
        var cpuCores = 0d;
        var allocRate = 0d;

        if (_hasBaseline)
        {
            var elapsed = nowUtc - _previousUtc;
            cpuCores = ResourceCpu.CoresFromProcessorTime(_previousCpu, cpu, elapsed);
            if (elapsed > TimeSpan.Zero && allocated >= _previousAllocatedBytes)
                allocRate = (allocated - _previousAllocatedBytes) / elapsed.TotalSeconds;
        }

        _previousCpu = cpu;
        _previousUtc = nowUtc;
        _previousAllocatedBytes = allocated;
        _hasBaseline = true;

        ThreadPool.GetMaxThreads(out var maxWorker, out _);
        ThreadPool.GetAvailableThreads(out var availableWorker, out _);
        var busy = Math.Max(0, maxWorker - availableWorker);

        var heap = GC.GetGCMemoryInfo().HeapSizeBytes;

        return new ProcessSnapshot(
            ResourceCpu.BytesToMib(_process.WorkingSet64),
            ResourceCpu.BytesToMib(heap),
            cpuCores,
            allocRate,
            GC.CollectionCount(2),
            busy,
            ThreadPool.PendingWorkItemCount,
            Math.Max(_counters.KestrelConnections, KestrelTcpConnections.CountEstablished()),
            _counters.CurrentRequests);
    }

    public void Dispose()
    {
        _counters.Dispose();
        _process.Dispose();
    }
}

internal sealed record ProcessSnapshot(
    double RssMib,
    double GcHeapMib,
    double CpuCores,
    double AllocRateBytesPerSecond,
    int Gen2Collections,
    int ThreadPoolBusy,
    long ThreadPoolQueue,
    int KestrelConnections,
    int CurrentHttpRequests);
