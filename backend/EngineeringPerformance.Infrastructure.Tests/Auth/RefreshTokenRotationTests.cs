using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Auth;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Tests.Auth;

/// <summary>
/// Ротация refresh-токена при параллельных запросах из нескольких вкладок (ISS-017).
/// </summary>
public sealed class RefreshTokenRotationTests
{
    [Fact]
    public async Task ValidateAndRotate_SecondCallWithSameToken_SucceedsWithinGracePeriod()
    {
        await using var context = CreateContext();
        var user = await AddUserAsync(context);
        var service = CreateService(context);
        var initial = await service.CreateAsync(user.Id, ipAddress: null, userAgent: null);

        var first = await service.ValidateAndRotateAsync(initial.Token, ipAddress: null, userAgent: null);
        var second = await service.ValidateAndRotateAsync(initial.Token, ipAddress: null, userAgent: null);

        first.Should().NotBeNull();
        second.Should().NotBeNull("вторая вкладка не должна разлогинивать пользователя");
        second!.Token.Should().NotBe(first!.Token);
    }

    [Fact]
    public async Task ValidateAndRotate_ConcurrentCalls_BothSucceed()
    {
        await using var context = CreateContext();
        var user = await AddUserAsync(context);
        var service = CreateService(context);
        var initial = await service.CreateAsync(user.Id, ipAddress: null, userAgent: null);

        // Последовательные вызовы одним DbContext моделируют две вкладки с общей cookie jar.
        var rotations = new List<RefreshTokenRotation?>
        {
            await service.ValidateAndRotateAsync(initial.Token, ipAddress: null, userAgent: null),
            await service.ValidateAndRotateAsync(initial.Token, ipAddress: null, userAgent: null),
            await service.ValidateAndRotateAsync(initial.Token, ipAddress: null, userAgent: null),
        };

        rotations.Should().OnlyContain(rotation => rotation != null);
        rotations.Select(rotation => rotation!.Token).Distinct().Should().HaveCount(3);
    }

    [Fact]
    public async Task ValidateAndRotate_RotatedTokenOutsideGracePeriod_IsRejected()
    {
        await using var context = CreateContext();
        var user = await AddUserAsync(context);
        var service = CreateService(context);
        var initial = await service.CreateAsync(user.Id, ipAddress: null, userAgent: null);
        await service.ValidateAndRotateAsync(initial.Token, ipAddress: null, userAgent: null);

        var rotated = await context.RefreshTokens.SingleAsync(token => token.RevokedAt != null);
        rotated.RevokedAt = DateTime.UtcNow.AddMinutes(-5);
        await context.SaveChangesAsync();

        var reuse = await service.ValidateAndRotateAsync(initial.Token, ipAddress: null, userAgent: null);

        reuse.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAndRotate_AfterLogout_IsRejectedEvenWithinGracePeriod()
    {
        await using var context = CreateContext();
        var user = await AddUserAsync(context);
        var service = CreateService(context);
        var initial = await service.CreateAsync(user.Id, ipAddress: null, userAgent: null);
        var rotation = await service.ValidateAndRotateAsync(initial.Token, ipAddress: null, userAgent: null);

        await service.RevokeAsync(rotation!.Token, "Logout");
        var reuse = await service.ValidateAndRotateAsync(initial.Token, ipAddress: null, userAgent: null);

        reuse.Should().BeNull("после logout цепочка токенов недействительна");
    }

    [Fact]
    public async Task ValidateAndRotate_LinksReplacementInSingleSave()
    {
        await using var context = CreateContext();
        var user = await AddUserAsync(context);
        var service = CreateService(context);
        var initial = await service.CreateAsync(user.Id, ipAddress: null, userAgent: null);

        await service.ValidateAndRotateAsync(initial.Token, ipAddress: null, userAgent: null);

        var rotated = await context.RefreshTokens.SingleAsync(token => token.RevokedAt != null);
        rotated.RevocationReason.Should().Be("Rotated");
        rotated.ReplacedByTokenId.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAndRotate_InactiveUser_IsRejected()
    {
        await using var context = CreateContext();
        var user = await AddUserAsync(context);
        var service = CreateService(context);
        var initial = await service.CreateAsync(user.Id, ipAddress: null, userAgent: null);

        user.IsActive = false;
        await context.SaveChangesAsync();

        var rotation = await service.ValidateAndRotateAsync(initial.Token, ipAddress: null, userAgent: null);

        rotation.Should().BeNull();
    }

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static RefreshTokenService CreateService(AppDbContext context) =>
        new(context, Options.Create(new JwtSettings { RefreshTokenLifetimeDays = 7 }));

    private static async Task<User> AddUserAsync(AppDbContext context)
    {
        var user = new User
        {
            Username = "refresh-user",
            IsActive = true,
            Role = new Role { Name = "Administrator" },
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
}
