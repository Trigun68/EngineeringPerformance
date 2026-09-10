using ClosedXML.Excel;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Infrastructure.Export;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Export;

public sealed class CompareTableDocumentBuilderTests
{
    [Fact]
    public void Summary_has_grid_columns_opt_base_pes_and_plan_z()
    {
        var row = new VersionCompareRowDto(
            1,
            null,
            "IIC-1 — Бетон (м³)",
            FactZScoreA: 0.1m,
            FactZScoreB: 0.2m,
            FactZDelta: 0.1m,
            FactPhysicalVolumeA: 10m,
            FactPhysicalVolumeB: 12m,
            FactPhysicalVolumeDisplayA: "10,00 м³",
            PhysicalVolumeA: 10m,
            PhysicalVolumeB: 12m,
            PhysicalVolumeDisplayA: "10,00 м³",
            AssessmentPhysicalVolumeA: 10m,
            AssessmentPhysicalVolumeB: 12m,
            PlannedLaborHoursA: 100m,
            PlannedLaborHoursB: 110m,
            PlannedLaborHoursOptimisticA: 80m,
            PlannedLaborHoursOptimisticB: 90m,
            PlannedLaborHoursPessimisticA: 120m,
            PlannedLaborHoursPessimisticB: 130m,
            PlannedProductivityA: 0.1m,
            PlannedProductivityB: 0.11m,
            FactLaborHoursA: 50m,
            FactLaborHoursB: 55m,
            FactProductivityA: 0.2m,
            FactProductivityB: 0.22m,
            PlanZScoreA: -0.3m,
            PlanZScoreB: -0.2m,
            PlanZDelta: 0.1m);

        var document = CompareTableDocumentBuilder.BuildSummary([row], "Сравнение версий: P (A / B)");
        var columns = document.Sections[0].Columns;
        columns.Should().HaveCount(23);
        columns[0].Header.Should().Be("Направление");
        columns[1].Header.Should().Be("Физобъём (v. А)");
        columns[3].Header.Should().Be("Сгруппированный физобъём (v. А)");
        columns[4].Header.Should().Be("Сгруппированный физобъём (v. Б)");
        columns[5].Header.Should().Be("План.Труд. (Опт. v. А)");
        columns[6].Header.Should().Be("План.Труд. (Баз. v. А)");
        columns[7].Header.Should().Be("План.Труд. (Песс. v. А)");
        columns[^3].Header.Should().Be("Оценка Z плана (v. А)");

        var cells = document.Sections[0].Rows[0].Cells;
        cells.Should().HaveCount(23);
        cells[0].Text.Should().Be("IIC-1 — Бетон (м³)");
        cells[5].Number.Should().Be(80m);
        cells[5].Tone.Should().Be(ForecastHourTone.Optimistic);
        cells[6].Number.Should().Be(100m);
        cells[6].Tone.Should().Be(ForecastHourTone.Base);
        cells[7].Number.Should().Be(120m);
        cells[7].Tone.Should().Be(ForecastHourTone.Pessimistic);
        cells[8].Number.Should().Be(90m);
        cells[10].Number.Should().Be(130m);
        cells[^1].Number.Should().Be(0.1m);
    }

    [Fact]
    public void Facility_maps_planned_hours_a_as_base()
    {
        var row = new VersionCompareFacilityRowDto(
            "F01",
            "Объект",
            FactPhysicalVolumeA: 2m,
            FactPhysicalVolumeB: 3m,
            PlannedLaborHoursA: 40m,
            PlannedLaborHoursB: 44m,
            PlannedLaborHoursOptimisticA: 30m,
            PlannedLaborHoursOptimisticB: 33m,
            PlannedLaborHoursPessimisticA: 50m,
            PlannedLaborHoursPessimisticB: 55m,
            FactLaborHoursA: 20m,
            FactLaborHoursB: 22m,
            ZScoreA: 0.4m,
            ZScoreB: 0.5m,
            ZDelta: 0.1m);

        var document = CompareTableDocumentBuilder.BuildFacility([row], "Сравнение по объектам");
        document.Sections[0].Columns.Should().HaveCount(19);
        var cells = document.Sections[0].Rows[0].Cells;
        cells[0].Text.Should().Be("F01");
        cells[4].Number.Should().Be(30m);
        cells[4].Tone.Should().Be(ForecastHourTone.Optimistic);
        cells[5].Number.Should().Be(40m);
        cells[5].Tone.Should().Be(ForecastHourTone.Base);
        cells[6].Number.Should().Be(50m);
        cells[^1].Number.Should().Be(0.1m);
    }

    [Fact]
    public void Detail_has_full_grid_columns_not_summary_shape()
    {
        var row = new VersionCompareDetailRowDto(
            "F01",
            "Объект",
            "Комплект",
            "PV-1",
            "ФО",
            "W1",
            "Работа",
            QuantityA: 2m,
            QuantityB: 3m,
            PlannedLaborHoursA: 10m,
            PlannedLaborHoursB: 11m,
            PlannedLaborHoursOptimisticA: 8m,
            PlannedLaborHoursOptimisticB: 9m,
            PlannedLaborHoursPessimisticA: 12m,
            PlannedLaborHoursPessimisticB: 13m,
            FactLaborHoursA: 5m,
            FactLaborHoursB: 6m,
            ZScoreA: 0.1m,
            ZScoreB: 0.2m,
            ZDelta: 0.1m,
            IicGroupName: "IIC-1 — Бетон (м³)",
            IicUnitName: "м³",
            Presence: "A+B",
            BindingType: "KFO",
            CatalogId: "CAT-1",
            Tag: "T-1",
            ItemName: "",
            WorkComment: "комментарий",
            EstimationBasis: "основание",
            IsAssessment: "Да",
            Coefficient: 1.1m,
            NormProductivity: 0.5m,
            NormStdDev: 0.02m,
            IsEquipment: true);

        var document = CompareTableDocumentBuilder.BuildDetail([row], "Детализация сравнения");
        var columns = document.Sections[0].Columns;
        columns.Should().HaveCount(37);
        columns.Select(c => c.Header).Should().Contain("Тип привязки");
        columns.Select(c => c.Header).Should().Contain("TAG");
        columns.Select(c => c.Header).Should().Contain("Оборудование");
        columns.Select(c => c.Header).Should().Contain("Δ оценка Z");
        columns.Should().NotContain(c => c.Header == "Код позиции");
        columns.Count.Should().BeGreaterThan(24);

        var cells = document.Sections[0].Rows[0].Cells;
        cells.Should().HaveCount(37);
        cells[2].Text.Should().Be("KFO");
        cells[5].Text.Should().Be("T-1");
        cells[7].Text.Should().Be("Работа");
        cells[18].Text.Should().Be("Да");
        cells[21].Number.Should().Be(8m);
        cells[22].Number.Should().Be(10m);
        cells[22].Tone.Should().Be(ForecastHourTone.Base);
        cells[^2].Number.Should().Be(0.1m);
        cells[^1].Text.Should().Be("A+B");

        var excel = ForecastTableRenderers.ToExcel(document);
        using var stream = new MemoryStream(excel);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        sheet.Cell(3, 3).GetString().Should().Be("Тип привязки");
        sheet.Cell(3, 6).GetString().Should().Be("TAG");
        sheet.Cell(3, 19).GetString().Should().Be("Оборудование");
        sheet.Cell(3, 37).GetString().Should().Be("Наличие");
        sheet.Cell(4, 19).GetString().Should().Be("Да");
        sheet.Column(2).Width.Should().BeGreaterThan(8.43);
        var optFill = sheet.Cell(4, 22).Style.Fill.BackgroundColor.Color;
        optFill.R.Should().Be(0xE3);
        optFill.G.Should().Be(0xF2);
        optFill.B.Should().Be(0xFD);
    }
}
