using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

public sealed class AppSettingsTests : IDisposable
{
    private readonly string _tempFile;

    public AppSettingsTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"ep-settings-{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    [Fact]
    public void ConnectionString_NoFile_ReturnsDefault()
    {
        var settings = new AppSettings(_tempFile);

        settings.ConnectionString.Should().Contain("Server=.\\SQLEXPRESS");
        settings.ConnectionString.Should().NotContain("TrustServerCertificate=True",
            because: "по умолчанию TrustServerCertificate должен быть выключен (P2)");
    }

    [Fact]
    public void AutoLoginEnabled_NoFile_ReturnsFalse()
    {
        var settings = new AppSettings(_tempFile);

        settings.AutoLoginEnabled.Should().BeFalse(
            because: "auto-login должен быть выключен по умолчанию (P5)");
    }

    [Fact]
    public void SaveConnectionString_PersistsAcrossInstances()
    {
        var first = new AppSettings(_tempFile);
        first.SaveConnectionString("Server=.;Database=Test;Integrated Security=true;");

        var second = new AppSettings(_tempFile);
        second.ConnectionString.Should().Be("Server=.;Database=Test;Integrated Security=true;");
    }

    [Fact]
    public void SaveAutoLoginEnabled_PersistsAcrossInstances()
    {
        var first = new AppSettings(_tempFile);
        first.SaveAutoLoginEnabled(true);

        var second = new AppSettings(_tempFile);
        second.AutoLoginEnabled.Should().BeTrue();
    }

    [Fact]
    public void Reload_RereadsFileFromDisk()
    {
        var settings = new AppSettings(_tempFile);
        settings.SaveConnectionString("Server=A;Database=A;");

        // Внешняя запись (имитация ручного редактирования файла).
        File.WriteAllText(_tempFile, "{\"ConnectionString\":\"Server=B;Database=B;\",\"AutoLoginEnabled\":true}");

        settings.Reload();

        settings.ConnectionString.Should().Be("Server=B;Database=B;");
        settings.AutoLoginEnabled.Should().BeTrue();
    }

    [Fact]
    public void Load_CorruptedJson_FallsBackToDefault()
    {
        File.WriteAllText(_tempFile, "{ this is not valid json");

        var settings = new AppSettings(_tempFile);

        settings.ConnectionString.Should().Contain("EngineeringPerformance");
    }
}
