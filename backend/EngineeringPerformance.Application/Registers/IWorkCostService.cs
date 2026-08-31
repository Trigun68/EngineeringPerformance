namespace EngineeringPerformance.Application.Registers;

public interface IWorkCostService
{
    Task<IReadOnlyList<WorkCostRowDto>> GetAsOfAsync(DateTime? asOf, CancellationToken cancellationToken = default);

    /// <summary>Полное дерево ИИС со ставками, записанными именно на эту дату «Действует с».</summary>
    Task<IReadOnlyList<WorkCostRowDto>> GetByEffectiveFromAsync(
        DateTime effectiveFrom,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkCostPeriodDto>> ListPeriodsAsync(CancellationToken cancellationToken = default);

    Task<WorkCostBatchResult> SaveBatchAsync(WorkCostBatchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет все ставки с данной датой начала и продлевает предыдущий период
    /// (EffectiveTo = дата начала − 1) до EffectiveTo удалённого блока.
    /// </summary>
    Task<WorkCostPeriodDeleteResult> DeletePeriodAsync(
        DateTime effectiveFrom,
        CancellationToken cancellationToken = default);
}
