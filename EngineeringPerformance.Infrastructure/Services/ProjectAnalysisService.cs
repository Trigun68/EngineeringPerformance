using EngineeringPerformance.Core;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class ProjectAnalysisService : IProjectAnalysisService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IPerformanceService _performanceService;
    private readonly ProjectCharacteristicSelectionService _characteristicSelectionService;

    public ProjectAnalysisService(
        IDbContextFactory<AppDbContext> contextFactory,
        IPerformanceService performanceService,
        ProjectCharacteristicSelectionService characteristicSelectionService)
    {
        _contextFactory = contextFactory;
        _performanceService = performanceService;
        _characteristicSelectionService = characteristicSelectionService;
    }

    public async Task<ProjectAnalysisReportDto> GenerateReportAsync(int projectId, int versionId)
    {
        var performanceData = await _performanceService.GetPerformanceDataAsync(projectId, versionId);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var version = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .Include(v => v.Project)
            .FirstOrDefaultAsync(v => v.Id == versionId && v.ProjectId == projectId);

        if (version is null)
        {
            throw new InvalidOperationException($"Version {versionId} not found for project {projectId}");
        }

        // Корневые узлы (Level=1) используем только для расчёта суммарных трудозатрат и индикатора PEPM,
        // чтобы не суммировать один и тот же вклад дважды (родитель + потомок).
        var rootDisciplines = performanceData.Where(r => r.Level == 1).ToList();
        var totalLaborHours = rootDisciplines.Sum(r => r.FactLaborHours ?? 0m);

        // В таблицу «Анализ по дисциплинам» отдаём всю иерархию справочника IIC
        // (все уровни, с ParentId), чтобы UI мог построить TreeListView.
        var disciplines = performanceData
            .Where(r => r.WorkDirectionId >= 0)
            .Select(r => CreateDisciplineDto(r, totalLaborHours))
            .ToList();

        var rootDisciplineDtos = disciplines
            .Where(d => d.ParentId is null)
            .ToList();

        var permIndicator = CalculatePermIndicator(rootDisciplineDtos);
        var interpretation = GetInterpretation(permIndicator);
        var percentile = GetPercentile(permIndicator);
        var status = GetStatus(permIndicator);

        // Точность оценки — средняя PD% по характеристикам проекта (как в окне проекта:
        // `ProjectEditViewModel.LoadProjectDeviationAsync`). Качественная оценка берётся
        // из общего справочника `ComparisonAccuracy` по тем же порогам <7 / <9 / <11.
        var characteristicRows = await _characteristicSelectionService.GetByProjectAsync(projectId);
        var accuracyScore = CalculateAccuracyFromRows(characteristicRows);
        var accuracyInterpretation = ComparisonAccuracy.FromValuePercent(accuracyScore);
        var accuracyCharacteristics = characteristicRows
            .Select(r => new AccuracyCharacteristicDto(r.CharacteristicName, r.OptionName, r.ValuePercent))
            .ToList();

        var keyDisciplines = rootDisciplineDtos
            .Where(d => d.ZScore > 0.5m)
            .OrderByDescending(d => d.ZScore)
            .Select(d => d.DisciplineName)
            .ToList();

        var recommendations = GenerateRecommendations(rootDisciplineDtos, permIndicator);

        return new ProjectAnalysisReportDto(
            ProjectName: version.Project.Name,
            ProjectCode: version.Project.Code,
            VersionName: version.VersionName,
            PermIndicator: permIndicator,
            Interpretation: interpretation,
            Percentile: percentile,
            Status: status,
            TotalLaborHours: totalLaborHours,
            Disciplines: disciplines,
            AccuracyScore: accuracyScore,
            AccuracyInterpretation: accuracyInterpretation,
            AccuracyCharacteristics: accuracyCharacteristics,
            Recommendations: recommendations,
            KeyDisciplines: keyDisciplines);
    }

    private static DisciplineAnalysisDto CreateDisciplineDto(
        PerformanceRowDto row,
        decimal totalLaborHours)
    {
        var laborHours = row.FactLaborHours ?? 0m;
        var percentOfTotal = totalLaborHours > 0 ? (laborHours / totalLaborHours) * 100m : 0m;
        var assessment = row.Interpretation?.ToRussian() ?? "Нет данных";

        return new DisciplineAnalysisDto(
            Id: row.WorkDirectionId,
            ParentId: row.ParentId,
            DisciplineName: row.WorkDirectionName,
            LaborHours: laborHours,
            PercentOfTotal: Math.Round(percentOfTotal, 1),
            ZScore: row.ZScore,
            Assessment: assessment);
    }

    private static decimal? CalculateAccuracyFromRows(IReadOnlyList<ProjectCharacteristicSelectionRow> rows)
    {
        var values = rows
            .Where(r => r.ValuePercent.HasValue)
            .Select(r => r.ValuePercent!.Value)
            .ToList();

        return values.Count == 0
            ? null
            : Math.Round(values.Average(), 2, MidpointRounding.AwayFromZero);
    }

    private static decimal? CalculatePermIndicator(IReadOnlyList<DisciplineAnalysisDto> disciplines)
    {
        var disciplinesWithZScore = disciplines.Where(d => d.ZScore.HasValue && d.LaborHours > 0).ToList();

        if (disciplinesWithZScore.Count == 0)
        {
            return null;
        }

        var totalWeightedHours = disciplinesWithZScore.Sum(d => d.ZScore!.Value * d.LaborHours);
        var totalHours = disciplinesWithZScore.Sum(d => d.LaborHours);

        return totalHours > 0 ? Math.Round(totalWeightedHours / totalHours, 2) : null;
    }

    private static string GetInterpretation(decimal? permIndicator)
    {
        if (!permIndicator.HasValue)
        {
            return "Недостаточно данных";
        }

        return permIndicator.Value switch
        {
            < -1.0m => "Отличная производительность",
            < -0.5m => "Хорошая производительность",
            <= 0.5m => "Удовлетворительная производительность",
            <= 1.0m => "Требует внимания",
            _ => "Критически низкая производительность"
        };
    }

    private static string GetPercentile(decimal? permIndicator)
    {
        if (!permIndicator.HasValue)
        {
            return "Н/Д";
        }

        var z = permIndicator.Value;

        // Диапазон PEPM [-1, 1] линейно отображается на перцентили [0, 100].
        // Шаг 0.2 → 10 равных корзин; значения вне [-1, 1] клэмпятся к границам.
        // Напоминание: меньший PEPM = более высокая производительность (меньше часов на единицу).
        // Шкала перцентилей идёт в ту же сторону, что и PEPM: низкий PEPM = низкий перцентиль,
        // что соответствует нижнему хвосту распределения Z по нормали.
        if (z < -1.0m) return "0-й";
        if (z < -0.8m) return "0-10-й";
        if (z < -0.6m) return "10-20-й";
        if (z < -0.4m) return "20-30-й";
        if (z < -0.2m) return "30-40-й";
        if (z < 0m) return "40-50-й";
        if (z < 0.2m) return "50-60-й";
        if (z < 0.4m) return "60-70-й";
        if (z < 0.6m) return "70-80-й";
        if (z < 0.8m) return "80-90-й";
        if (z <= 1.0m) return "90-100-й";
        return "100-й";
    }

    private static string GetStatus(decimal? permIndicator)
    {
        if (!permIndicator.HasValue)
        {
            return "Нет данных";
        }

        return permIndicator.Value switch
        {
            < -0.5m => "В норме",
            <= 0.5m => "Стабильный",
            <= 1.0m => "Требует внимания",
            _ => "Критический"
        };
    }

    private static IReadOnlyList<string> GenerateRecommendations(
        IReadOnlyList<DisciplineAnalysisDto> disciplines,
        decimal? permIndicator)
    {
        var recommendations = new List<string>();

        var problemDisciplines = disciplines
            .Where(d => d.ZScore > 0.5m)
            .OrderByDescending(d => d.ZScore)
            .ToList();

        if (problemDisciplines.Count > 0)
        {
            var topProblem = problemDisciplines.First();
            recommendations.Add(
                $"Обратить внимание на направление \"{topProblem.DisciplineName}\" " +
                $"(Z-оценка: {topProblem.ZScore:F2}) — превышение нормативов трудозатрат");
        }

        if (permIndicator.HasValue && permIndicator.Value > 1.0m)
        {
            recommendations.Add(
                "Общий показатель PEPM превышает критический порог. " +
                "Рекомендуется провести детальный анализ причин отклонений");
        }

        var excellentDisciplines = disciplines
            .Where(d => d.ZScore < -1.0m)
            .ToList();

        if (excellentDisciplines.Count > 0)
        {
            var bestDiscipline = excellentDisciplines.OrderBy(d => d.ZScore).First();
            recommendations.Add(
                $"Изучить лучшие практики направления \"{bestDiscipline.DisciplineName}\" " +
                $"(Z-оценка: {bestDiscipline.ZScore:F2}) для тиражирования опыта");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Продолжать мониторинг показателей производительности");
        }

        return recommendations;
    }
}
