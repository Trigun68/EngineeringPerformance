using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

public sealed class ConnectionStringMaskingTests
{
    [Fact]
    public void MaskForDisplay_ReplacesSqlPassword()
    {
        var masked = ConnectionStringMasking.MaskForDisplay(
            "Server=.;Database=Db;User Id=sa;Password=Secret123;TrustServerCertificate=True");

        masked.Should().Contain("Password=****");
        masked.Should().NotContain("Secret123");
    }

    [Fact]
    public void MaskForDisplay_Empty_ReturnsEmpty()
    {
        ConnectionStringMasking.MaskForDisplay(null).Should().BeEmpty();
        ConnectionStringMasking.MaskForDisplay("   ").Should().BeEmpty();
    }
}
