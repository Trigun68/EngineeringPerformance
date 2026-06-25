using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Api.Tests;

public sealed class LoginIdentityMatcherTests
{
    [Theory]
    [InlineData("admin", "admin", null, true)]
    [InlineData("ADMIN", "admin", null, true)]
    [InlineData("eliseev", "eliseev", @"LAPTOP-ANTON\elise", true)]
    [InlineData(@"LAPTOP-ANTON\elise", "eliseev", @"LAPTOP-ANTON\elise", true)]
    [InlineData("elise", "eliseev", @"LAPTOP-ANTON\elise", true)]
    [InlineData("unknown", "admin", null, false)]
    public void Matches_ResolvesUsernameAndWindowsVariants(
        string input,
        string username,
        string? windowsUsername,
        bool expected)
    {
        var user = new User
        {
            Username = username,
            WindowsUsername = windowsUsername,
        };

        Assert.Equal(expected, LoginIdentityMatcher.Matches(user, input));
    }
}
