using EngineeringPerformance.Infrastructure.Analytics;
using FluentAssertions;
using Xunit;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

public sealed class AnalyticsSqlGateTests
{
    [Fact]
    public async Task WaitAsync_AllowsTwoConcurrent_BlocksThird()
    {
        var gate = new AnalyticsSqlGate();

        await gate.WaitAsync(CancellationToken.None);
        await gate.WaitAsync(CancellationToken.None);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var third = gate.WaitAsync(cts.Token);

        var act = async () => await third;
        // Третий ждёт: параллелизм ограничен двумя тяжёлыми загрузками.
        await act.Should().ThrowAsync<OperationCanceledException>();

        gate.Release();
        await gate.WaitAsync(CancellationToken.None); // после Release слот освобождается
        gate.Release();
        gate.Release();
    }

    [Fact]
    public async Task WaitAsync_CancellationBeforeAcquire_Throws()
    {
        var gate = new AnalyticsSqlGate();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await gate.WaitAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
