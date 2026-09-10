using EngineeringPerformance.Application.Registers;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Registers;

public sealed class DocSetCodeParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("без-разделителя")]
    [InlineData("тип|")]
    public void Extract_ReturnsNull_WhenNoDocSet(string? code)
    {
        DocSetCodeParser.Extract(code).Should().BeNull();
    }

    [Fact]
    public void Extract_ReturnsPartAfterFirstPipe()
    {
        DocSetCodeParser.Extract("тип документа|ДС-1").Should().Be("ДС-1");
        DocSetCodeParser.Extract("a|b|c").Should().Be("b|c");
    }
}
