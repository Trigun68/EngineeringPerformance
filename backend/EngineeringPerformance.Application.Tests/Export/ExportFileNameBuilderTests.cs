using EngineeringPerformance.Application.Export;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Export;

public sealed class ExportFileNameBuilderTests
{
    [Fact]
    public void Build_ProjectReport_UsesRussianTemplate()
    {
        var name = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Forecast,
            "xlsx",
            ExportFileNameBuilder.Views.Summary,
            projectCode: "25-310",
            projectName: "Реконструкция НС",
            versionLabel: "Основная");

        name.Should().Be("прогноз_итого_-_25-310_Реконструкция_НС_(Основная).xlsx");
    }

    [Fact]
    public void Build_ReplacesInvalidCharacters_AndKeepsHyphenInCode()
    {
        var name = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Fact,
            "pdf",
            ExportFileNameBuilder.Views.Facility,
            projectCode: "25-310",
            projectName: "Объект: А/Б (черновик)",
            versionLabel: "v1 (draft)");

        name.Should().Be("факт_по_объектам_-_25-310_Объект_А_Б_черновик_(v1_draft).pdf");
    }

    [Fact]
    public void CombineVersions_JoinsDistinctSanitizedNames()
    {
        var combined = ExportFileNameBuilder.CombineVersions("Основная", "Факт 2025", "Основная");
        var name = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Compare,
            "pdf",
            ExportFileNameBuilder.Views.Summary,
            projectCode: "P1",
            projectName: "Проект",
            versionLabel: combined);

        name.Should().Be("сравнение_итого_-_P1_Проект_(Основная_Факт_2025).pdf");
    }

    [Fact]
    public void Build_WithoutProject_OmitsDashBlock()
    {
        var name = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Directory,
            "xlsx",
            ExportFileNameBuilder.DirectoryView("iic-work-directions"));

        name.Should().Be("справочник_группы_IIC.xlsx");
    }

    [Fact]
    public void Build_QualifierVisible_InsertedAfterView()
    {
        var name = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Forecast,
            "xlsx",
            ExportFileNameBuilder.Views.Detail,
            ExportFileNameBuilder.Qualifiers.Visible,
            "P1",
            "Проект",
            "v2");

        name.Should().Be("прогноз_детально_выделенные_-_P1_Проект_(v2).xlsx");
    }

    [Fact]
    public void Build_TruncatesLongProjectName_KeepsLimit()
    {
        var longName = new string('А', 250);
        var fileName = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Report,
            "pdf",
            projectCode: "25-310",
            projectName: longName,
            versionLabel: "Основная");

        fileName.Length.Should().BeLessThanOrEqualTo(ExportFileNameBuilder.MaxFileNameLength);
        fileName.Should().StartWith("отчет_-_25-310_");
        fileName.Should().EndWith("_(Основная).pdf");
    }

    [Fact]
    public void CompareView_MapsKnownSections()
    {
        ExportFileNameBuilder.CompareView("summary").Should().Be("итого");
        ExportFileNameBuilder.CompareView("facility").Should().Be("по_объектам");
        ExportFileNameBuilder.CompareView("detail").Should().Be("детально");
        ExportFileNameBuilder.CompareView("full").Should().BeNull();
    }

    [Fact]
    public void DirectoryView_MapsKnownSlugs()
    {
        ExportFileNameBuilder.DirectoryView("iic-work-direction-norms").Should().Be("нормы_IIC");
        ExportFileNameBuilder.DirectoryView("work-costs").Should().Be("стоимость_работ");
        ExportFileNameBuilder.DirectoryView("equipment").Should().Be("оборудование");
    }

    [Fact]
    public void Build_ReportWithoutView_HasOnlyModuleAndProject()
    {
        var name = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Report,
            "pdf",
            projectCode: "25-310",
            projectName: "Реконструкция НС",
            versionLabel: "Основная_Факт_2025");

        name.Should().Be("отчет_-_25-310_Реконструкция_НС_(Основная_Факт_2025).pdf");
    }

    [Fact]
    public void Build_ProductivityTrend_UsesAnalyticsModule()
    {
        var name = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Analytics,
            "xlsx",
            ExportFileNameBuilder.Views.ProductivityTrend);

        name.Should().Be("аналитика_тренд_производительности.xlsx");
    }

    [Fact]
    public void Build_ProjectTrend_UsesReportModuleAndTrendView()
    {
        var name = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Report,
            "xlsx",
            ExportFileNameBuilder.Views.Trend,
            projectCode: "25-310",
            projectName: "Реконструкция НС",
            versionLabel: "Основная");

        name.Should().Be("отчет_тренд_-_25-310_Реконструкция_НС_(Основная).xlsx");
    }

    [Fact]
    public void Build_AnalyticsTrend_UsesAnalyticsModuleAndTrendView()
    {
        var name = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Analytics,
            "pdf",
            ExportFileNameBuilder.Views.Trend);

        name.Should().Be("аналитика_тренд.pdf");
    }
}
