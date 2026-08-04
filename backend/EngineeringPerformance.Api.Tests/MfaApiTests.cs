using System.Net;
using System.Net.Http.Json;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.Api.Tests;

public sealed class MfaApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    [Fact]
    public async Task Login_WithMfaEnabledUser_ReturnsMfaChallengeWithoutCookies()
    {
        using var availabilityClient = factory.CreateClient();
        if (!IntegrationTestAuth.EnsureSqlAvailable(availabilityClient))
            return;

        var username = $"mfa-test-{Guid.NewGuid():N}";
        const string password = "MfaTestPassword1!";
        int userId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var role = await context.Roles.AsNoTracking()
                .FirstAsync(candidate => candidate.Name == "Admin");
            var user = new User
            {
                Username = username,
                DisplayName = "MFA API Test",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                RoleId = role.Id,
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            userId = user.Id;

            var mfaService = scope.ServiceProvider.GetRequiredService<IMfaService>();
            await mfaService.EnableMfaAsync(user, "JBSWY3DPEHPK3PXP");
        }

        try
        {
            using var client = factory.CreateClient();
            var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { username, password });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
            login.Should().NotBeNull();
            login!.RequiresMfa.Should().BeTrue();
            login.MfaSessionToken.Should().NotBeNullOrWhiteSpace();
            response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeFalse();
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await context.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId);
            if (user is not null)
            {
                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }
        }
    }

    private sealed record LoginResponse(bool RequiresMfa, string? MfaSessionToken);
}
