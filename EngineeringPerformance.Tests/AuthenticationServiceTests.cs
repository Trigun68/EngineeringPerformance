using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public void Bcrypt_HashesAreVerified()
    {
        var password = "testpassword";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        BCrypt.Net.BCrypt.Verify(password, hash).Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("wrongpassword", hash).Should().BeFalse();
    }

    [Fact]
    public void Bcrypt_DifferentHashesForSamePassword()
    {
        var password = "testpassword";
        var hash1 = BCrypt.Net.BCrypt.HashPassword(password);
        var hash2 = BCrypt.Net.BCrypt.HashPassword(password);

        hash1.Should().NotBe(hash2);
        BCrypt.Net.BCrypt.Verify(password, hash1).Should().BeTrue();
        BCrypt.Net.BCrypt.Verify(password, hash2).Should().BeTrue();
    }

    [Fact]
    public void UserRole_EnumValuesAreCorrect()
    {
        ((int)UserRole.Administrator).Should().Be(1);
        ((int)UserRole.ProjectManager).Should().Be(2);
        ((int)UserRole.Observer).Should().Be(3);
    }

    [Fact]
    public void AccessLevel_EnumValuesAreCorrect()
    {
        ((int)AccessLevel.None).Should().Be(0);
        ((int)AccessLevel.Read).Should().Be(1);
        ((int)AccessLevel.Edit).Should().Be(2);
    }
}
