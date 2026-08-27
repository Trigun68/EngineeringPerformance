using FluentAssertions;
using EngineeringPerformance.Infrastructure.Diagnostics;
using Xunit;

namespace EngineeringPerformance.Infrastructure.Tests.Diagnostics;

public sealed class SqlQueryCommandInterceptorTests
{
    [Theory]
    [InlineData("SELECT * FROM [Projects]", "SELECT")]
    [InlineData("  insert into Equipments (Name) values (@p0)", "INSERT")]
    [InlineData("UPDATE dbo.Users SET Name = @p0", "UPDATE")]
    [InlineData("DELETE FROM ProjectEquipments WHERE Id = 1", "DELETE")]
    [InlineData("WITH cte AS (SELECT 1 AS x) SELECT * FROM cte", "SELECT")]
    public void DetectQueryType_RecognizesCrud(string sql, string expected)
    {
        SqlQueryCommandInterceptor.DetectQueryType(sql).Should().Be(expected);
    }

    [Fact]
    public void ExtractTables_FindsFromAndJoin()
    {
        var sql = @"SELECT p.Id FROM [Projects] p
                    INNER JOIN [ProjectAnalysisVersions] v ON v.ProjectId = p.Id";
        var tables = SqlQueryCommandInterceptor.ExtractTables(sql);
        tables.Should().Contain("Projects");
        tables.Should().Contain("ProjectAnalysisVersions");
    }

    [Fact]
    public void IsInternalMetricsSql_SkipsSelf()
    {
        SqlQueryCommandInterceptor.IsInternalMetricsSql("INSERT INTO [SqlQueryMetrics] ...")
            .Should().BeTrue();
        SqlQueryCommandInterceptor.IsInternalMetricsSql("SELECT * FROM Projects")
            .Should().BeFalse();
    }

    [Fact]
    public void ComputeHash_IsStableForSameSql()
    {
        var a = SqlQueryCommandInterceptor.ComputeHash(SqlQueryCommandInterceptor.NormalizeSql("SELECT 1 FROM Projects"));
        var b = SqlQueryCommandInterceptor.ComputeHash(SqlQueryCommandInterceptor.NormalizeSql("SELECT  1  FROM   Projects"));
        a.Should().Be(b);
        a.Should().HaveLength(64);
    }
}
