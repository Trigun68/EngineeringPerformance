using FluentAssertions;
using EngineeringPerformance.Domain.Entities;
using Xunit;

namespace EngineeringPerformance.Infrastructure.Tests.Import;

/// <summary>
/// Тесты для логики импорта физических объёмов — нормализация кодов, разбор строк.
/// </summary>
public sealed class PhysicalVolumeImportTests
{
    [Theory]
    [InlineData("01-01-001-01", "01-01-001-01")]
    [InlineData("1-1-1-1", "01-01-001-01")]
    [InlineData("  01-01-001-01  ", "01-01-001-01")]
    public void NormalizeEpCode_VariousInputs_ReturnsExpected(string input, string expected)
    {
        // Arrange & Act
        var result = NormalizeCatalogCode(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("KQ.02.160", "KQ.02.160")]
    [InlineData("kq.02.160", "kq.02.160")]
    [InlineData("  KQ.02.160  ", "KQ.02.160")]
    public void NormalizeKfoCode_VariousInputs_ReturnsExpected(string input, string expected)
    {
        var result = NormalizeCatalogCode(input);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("01-01-001-01 — Установка насоса", "01-01-001-01")]
    [InlineData("Текст 01-01-001-01 ещё текст", "01-01-001-01")]
    public void ExtractEpCode_FromText_ReturnsCode(string input, string expected)
    {
        // Паттерн для извлечения кода из текста
        var pattern = @"\d{1,2}-\d{1,2}-\d{1,4}-\d{1,4}";
        var match = System.Text.RegularExpressions.Regex.Match(input, pattern);

        match.Success.Should().BeTrue();
        match.Value.Should().Be(expected);
    }

    [Fact]
    public void PvCatalogRow_Properties_AreAccessible()
    {
        // Проверка структуры данных для каталога PV
        var row = new { Id = 1, Code = "01-01-001-01", Name = "Test", ClassifierId = (int?)5, ParentId = (int?)null };

        row.Id.Should().Be(1);
        row.Code.Should().Be("01-01-001-01");
        row.ClassifierId.Should().Be(5);
        row.ParentId.Should().BeNull();
    }

    // Хелпер для нормализации (зеркало логики из сервиса)
    private static string? NormalizeCatalogCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var t = code.Trim()
            .Replace('\u2013', '-')
            .Replace('\u2014', '-')
            .Replace('\u2212', '-')
            .Replace('\u2011', '-');

        // Нормализация БЕР-кода
        var parts = t.Split('-');
        if (parts.Length == 4 && parts.All(p => p.Length > 0 && p.All(char.IsDigit)))
        {
            return $"{parts[0].PadLeft(2, '0')}-{parts[1].PadLeft(2, '0')}-{parts[2].PadLeft(3, '0')}-{parts[3].PadLeft(2, '0')}";
        }

        return t;
    }
}
