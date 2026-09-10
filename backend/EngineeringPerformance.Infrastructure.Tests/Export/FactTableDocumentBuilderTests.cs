using ClosedXML.Excel;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Export;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Export;

public sealed class FactTableDocumentBuilderTests
{
    [Fact]
    public void Analysis_puts_project_total_first_and_hours_opt_base_pes()
    {
        var direction = new ProjectVersionDirectionDto(
            1, null, 0, "A1", "Направление", "м³",
            10m, 20m, 0.5m, null, 0.4m, null, 0.1m, 1.2m,
            PerformanceInterpretation.Good,
            [], [], [],
            PessimisticHours: 30m, BaseHours: 25m, OptimisticHours: 20m,
            PhysicalVolume: 10m, AssessmentPhysicalVolume: 14m);

        var report = new ProjectVersionAnalysisDto(
            "Проект", "P-01", "Факт v1",
            [direction],
            [],
            new ProjectVersionAnalysisSummaryDto(
                20m, 1.2m, "P80", PerformanceInterpretation.Good,
                [new PhysicalVolumeTotalByUnitDto("м³", 10m)],
                0m),
            new Dictionary<int, PlanningZParameters>());

        var document = FactTableDocumentBuilder.BuildAnalysis(report);
        var section = document.Sections.Should().ContainSingle().Subject;
        section.Columns.Select(c => c.Header).Should().Equal(
            "Шифр", "Направление", "Физобъём", "Сгруппированный физобъём", "Факт ч",
            "План.Труд.Оптим.", "План.Труд.Баз.", "План.Труд.Песс.",
            "Факт произв.", "Норм. произв.", "Z", "Интерпретация");

        section.Rows[0].IsTotal.Should().BeTrue();
        section.Rows[0].Cells[0].Text.Should().Be("По проекту");
        section.Rows[0].Cells[5].Number.Should().Be(20m);
        section.Rows[0].Cells[5].Tone.Should().Be(ForecastHourTone.Optimistic);
        section.Rows[0].Cells[6].Number.Should().Be(25m);
        section.Rows[0].Cells[6].Tone.Should().Be(ForecastHourTone.Base);
        section.Rows[0].Cells[7].Number.Should().Be(30m);
        section.Rows[0].Cells[7].Tone.Should().Be(ForecastHourTone.Pessimistic);

        section.Rows[1].Cells[0].Text.Should().Be("A1");
        section.Rows[1].Cells[2].Number.Should().Be(10m);
        section.Rows[1].Cells[3].Number.Should().Be(14m);
        section.Rows[1].Cells[5].Number.Should().Be(20m);
        section.Rows[1].Cells[6].Number.Should().Be(25m);
        section.Rows[1].Cells[7].Number.Should().Be(30m);
    }

    [Fact]
    public void Analysis_project_total_sums_plan_hours_of_descendants()
    {
        var parent = new ProjectVersionDirectionDto(
            10, null, 0, "P", "Родитель", "м³",
            null, null, null, null, 1.2m, null, null, null, null,
            [], [], [],
            PessimisticHours: 0m, BaseHours: 0m, OptimisticHours: 0m);
        var leaf = new ProjectVersionDirectionDto(
            11, 10, 1, "L", "Лист", "м³",
            8m, 12m, 0.5m, null, 1.2m, null, 0.1m, 1.2m,
            PerformanceInterpretation.Good,
            [], [], [],
            PessimisticHours: 60m, BaseHours: 50m, OptimisticHours: 40m);

        var report = new ProjectVersionAnalysisDto(
            "Проект", "P-01", "Факт v1",
            [parent, leaf],
            [],
            new ProjectVersionAnalysisSummaryDto(
                12m, 1.2m, "P80", PerformanceInterpretation.Good,
                [new PhysicalVolumeTotalByUnitDto("м³", 8m)],
                0m),
            new Dictionary<int, PlanningZParameters>());

        var document = FactTableDocumentBuilder.BuildAnalysis(report);
        var total = document.Sections[0].Rows[0];
        total.Cells[5].Number.Should().Be(40m);
        total.Cells[6].Number.Should().Be(50m);
        total.Cells[7].Number.Should().Be(60m);
        document.Sections[0].Rows[1].Cells[0].Text.Should().Be("P");
        document.Sections[0].Rows[2].Cells[0].Text.Should().Be("L");
    }

    [Fact]
    public void Summary_hours_are_opt_then_base_then_pes()
    {
        var row = new PerformanceRowDto(
            1, "Направление", "A1", null, 0, "м³",
            10m, 20m, 0.5m, null, 0.4m, null, 0.1m, 1.2m,
            PerformanceInterpretation.Good,
            OptimisticHours: 8m, BaseHours: 10m, PessimisticHours: 12m,
            PhysicalVolume: 10m, AssessmentPhysicalVolume: 14m);

        var document = FactTableDocumentBuilder.BuildSummary([row], "Оценка факта: P-01 (v1)");
        var cells = document.Sections[0].Rows[0].Cells;
        document.Sections[0].Columns[3].Header.Should().Be("Физобъём");
        document.Sections[0].Columns[4].Header.Should().Be("Сгруппированный физобъём");
        document.Sections[0].Columns[6].Header.Should().Be("План.Труд.Оптим.");
        document.Sections[0].Columns[7].Header.Should().Be("План.Труд.Баз.");
        document.Sections[0].Columns[8].Header.Should().Be("План.Труд.Песс.");
        cells[6].Number.Should().Be(8m);
        cells[6].Tone.Should().Be(ForecastHourTone.Optimistic);
        cells[7].Number.Should().Be(10m);
        cells[7].Tone.Should().Be(ForecastHourTone.Base);
        cells[8].Number.Should().Be(12m);
        cells[8].Tone.Should().Be(ForecastHourTone.Pessimistic);
        cells[3].Number.Should().Be(10m);
        cells[4].Number.Should().Be(14m);
    }

    [Fact]
    public void Analysis_uses_own_physical_volume_not_fact_remap()
    {
        var direction = new ProjectVersionDirectionDto(
            1, null, 0, "A1", "Направление", "м³",
            FactPhysicalVolume: 99m, FactLaborHours: 20m, FactProductivity: 0.5m, FactEstimate: null,
            NormProductivity: 0.4m, NormEstimate: null, NormStdDev: 0.1m, ZScore: 1.2m,
            Interpretation: PerformanceInterpretation.Good,
            PhysicalVolumes: [], EquipmentVolumes: [], DirectionLaborIntensities: [],
            PessimisticHours: 30m, BaseHours: 25m, OptimisticHours: 20m,
            PhysicalVolume: null, AssessmentPhysicalVolume: 14m);

        var report = new ProjectVersionAnalysisDto(
            "Проект", "P-01", "Факт v1",
            [direction],
            [],
            new ProjectVersionAnalysisSummaryDto(
                20m, 1.2m, "P80", PerformanceInterpretation.Good,
                [new PhysicalVolumeTotalByUnitDto("м³", 10m)],
                0m),
            new Dictionary<int, PlanningZParameters>());

        var cells = FactTableDocumentBuilder.BuildAnalysis(report).Sections[0].Rows[1].Cells;
        cells[2].Text.Should().Be("—");
        cells[2].Number.Should().BeNull();
        cells[3].Number.Should().Be(14m);
    }

    [Fact]
    public void Facility_hides_top10_when_empty_and_ranks_by_fact_hours()
    {
        FactTableDocumentBuilder.BuildFacility([], "Оценка факта по объектам").Sections.Should().ContainSingle();
        FactTableDocumentBuilder.BuildFacility([], "x").Sections[0].Title.Should().Be("Все объекты");

        var rows = Enumerable.Range(1, 12)
            .Select(i => new ProjectsPageFacilityRowDto(
                $"F{i:00}",
                $"Объект {i}",
                LaborHours: i * 10m,
                ZScore: null,
                Interpretation: string.Empty,
                BaseHours: 1m,
                OptimisticHours: 1m,
                PessimisticHours: 1m))
            .ToList();

        var document = FactTableDocumentBuilder.BuildFacility(rows, "Оценка факта по объектам: P (v1)");
        document.Sections.Should().HaveCount(2);
        document.Sections[0].Title.Should().Be("Топ-10 объектов");
        document.Sections[0].Rows.Should().HaveCount(10);
        document.Sections[0].Rows[0].Cells[0].Text.Should().Be("F12");
        document.Sections[1].Rows[^1].IsTotal.Should().BeTrue();
        document.Sections[1].Columns.Select(c => c.Header).Skip(2).Take(3).Should().Equal(
            "Факт.трудоёмк., ч", "План.Труд.Оптим.", "План.Труд.Баз.");
    }

    [Fact]
    public void Detail_has_26_columns_opt_base_pes_and_renders()
    {
        var row = new ProjectsPageDetailRowDto(
            1, "F01", "Объект", "Комплект", "CAT", "Позиция", "W1", "Работа", "IIC", "ч",
            2m, 10m, 8m, 12m, 5m, 0.4m, 1.1m, "норма", false,
            ItemName: "Наименование", PvName: "ФО");

        var document = FactTableDocumentBuilder.BuildDetail([row], "Детализация оценки факта: P (v1)");
        var columns = document.Sections[0].Columns;
        columns.Should().HaveCount(26);
        columns[19].Header.Should().Be("Факт.трудоёмк., ч");
        columns[^6].Header.Should().Be("План.Труд.Оптим.");
        columns[^5].Header.Should().Be("План.Труд.Баз.");
        columns[^4].Header.Should().Be("План.Труд.Песс.");
        columns[^3].Header.Should().Be("Факт производит.");
        columns[^2].Header.Should().Be("Оценка Z");

        document.Sections[0].Rows.Should().HaveCount(2);
        document.Sections[0].Rows[^1].IsTotal.Should().BeTrue();
        document.Sections[0].Rows[^1].Cells[0].Text.Should().Be("ИТОГО");

        var cells = document.Sections[0].Rows[0].Cells;
        cells[20].Number.Should().Be(8m);
        cells[20].Tone.Should().Be(ForecastHourTone.Optimistic);
        cells[21].Number.Should().Be(10m);
        cells[21].Tone.Should().Be(ForecastHourTone.Base);
        cells[22].Number.Should().Be(12m);

        var pdf = ForecastTableRenderers.ToPdf(
            document with { HeaderLines = TabularReportBanner.Lines("P", "Проект", "v1") });
        pdf.Should().NotBeEmpty();
        pdf[0].Should().Be((byte)'%');

        var excel = ForecastTableRenderers.ToExcel(
            document with { HeaderLines = TabularReportBanner.Lines("P", "Проект", "v1") });
        using var stream = new MemoryStream(excel);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        sheet.Cell(1, 1).GetString().Should().Be("Детализация оценки факта: P (v1)");
        sheet.Cell(2, 1).GetString().Should().StartWith("Проект:");
        sheet.Cell(3, 1).GetString().Should().StartWith("Версия анализа:");
    }
}
