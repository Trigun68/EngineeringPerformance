using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Auth;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace EngineeringPerformance.Infrastructure.Tests.Auth;

public sealed class AuthServiceTests
{
    private static readonly DateTime TokenExpiry = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);
    private const string Password = "S3cure!Pass";

    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly Mock<IRefreshTokenService> _refresh = new();
    private readonly Mock<IAccountLockoutService> _lockout = new();
    private readonly Mock<IMfaService> _mfa = new();
    private readonly Mock<IAuthAuditService> _audit = new();
    private readonly Mock<IUserActionLogger> _actionLogger = new();

    public AuthServiceTests()
    {
        _jwt.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("access-token");
        _jwt.Setup(j => j.GenerateMfaSessionToken(It.IsAny<User>())).Returns("mfa-session");
        _jwt.Setup(j => j.GetExpiration()).Returns(TokenExpiry);
        _refresh
            .Setup(r => r.CreateAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenIssue("refresh-token", TokenExpiry.AddDays(7)));
    }

    private AuthService CreateService(AppDbContext context) =>
        new(context, _jwt.Object, _refresh.Object, _lockout.Object, _mfa.Object, _audit.Object, _actionLogger.Object);

    private static async Task<AppDbContext> SeedUserAsync(Action<User>? configure = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);

        context.Roles.Add(new Role { Id = 2, Name = "Observer" });
        var user = new User
        {
            Id = 1,
            Username = "ivanov",
            DisplayName = "Иванов И.И.",
            RoleId = 2,
            IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
        };
        configure?.Invoke(user);
        context.Users.Add(user);
        context.AccessRights.Add(new AccessRight
        {
            Id = 1,
            RoleId = 2,
            ObjectName = "Projects",
            AccessLevel = AccessLevel.Read,
        });
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task LoginAsync_ValidPassword_ReturnsSuccessWithTokensAndPermissions()
    {
        await using var context = await SeedUserAsync();
        var service = CreateService(context);

        var result = await service.LoginAsync("ivanov", Password, "127.0.0.1", "agent");

        result.Status.Should().Be(AuthLoginStatus.Success);
        result.Tokens.Should().NotBeNull();
        result.Tokens!.AccessToken.Should().Be("access-token");
        result.Tokens.RefreshToken.Token.Should().Be("refresh-token");
        result.User!.Username.Should().Be("ivanov");
        result.User.Permissions.Should().ContainKey("Projects");
        result.RequiresMfaSetup.Should().BeFalse();

        var user = await context.Users.SingleAsync(u => u.Id == 1);
        user.LastLoginAt.Should().NotBeNull();
        _lockout.Verify(l => l.ResetFailedAttemptsAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.WriteAsync(
            AuthEventType.LoginSuccess, true, 1, "ivanov", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsInvalidCredentialsAndRecordsAttempt()
    {
        await using var context = await SeedUserAsync();
        var service = CreateService(context);

        var result = await service.LoginAsync("ivanov", "wrong", null, null);

        result.Status.Should().Be(AuthLoginStatus.InvalidCredentials);
        result.Tokens.Should().BeNull();
        _lockout.Verify(l => l.RecordFailedAttemptAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _refresh.Verify(
            r => r.CreateAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_UnknownUser_ReturnsInvalidCredentials_WithoutLockoutCalls()
    {
        await using var context = await SeedUserAsync();
        var service = CreateService(context);

        var result = await service.LoginAsync("nobody", Password, null, null);

        result.Status.Should().Be(AuthLoginStatus.InvalidCredentials);
        _lockout.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoginAsync_LockedOut_ReturnsLockedOut()
    {
        await using var context = await SeedUserAsync();
        _lockout
            .Setup(l => l.CheckLockoutAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService(context);

        var result = await service.LoginAsync("ivanov", Password, null, null);

        result.Status.Should().Be(AuthLoginStatus.LockedOut);
        _audit.Verify(a => a.WriteAsync(
            AuthEventType.LoginLockedOut, false, 1, "ivanov", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_MfaEnabled_ReturnsMfaRequired_WithoutTokens()
    {
        await using var context = await SeedUserAsync(u => u.MfaEnabled = true);
        _mfa.Setup(m => m.IsMfaEnabled(It.IsAny<User>())).Returns(true);
        var service = CreateService(context);

        var result = await service.LoginAsync("ivanov", Password, null, null);

        result.Status.Should().Be(AuthLoginStatus.MfaRequired);
        result.MfaSessionToken.Should().Be("mfa-session");
        result.Tokens.Should().BeNull();
        _refresh.Verify(
            r => r.CreateAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_InvalidToken_ReturnsNull()
    {
        await using var context = await SeedUserAsync();
        _refresh
            .Setup(r => r.ValidateAndRotateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenRotation?)null);
        var service = CreateService(context);

        var result = await service.RefreshAsync("stale", null, null);

        result.Should().BeNull();
        _audit.Verify(a => a.WriteAsync(
            AuthEventType.RefreshFailed, false, null, null, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_ReturnsRotatedTokens()
    {
        await using var context = await SeedUserAsync();
        var user = await context.Users.Include(u => u.Role).SingleAsync(u => u.Id == 1);
        _refresh
            .Setup(r => r.ValidateAndRotateAsync("ok", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenRotation(user, "rotated-refresh", TokenExpiry.AddDays(7)));
        var service = CreateService(context);

        var result = await service.RefreshAsync("ok", null, null);

        result.Should().NotBeNull();
        result!.Tokens.AccessToken.Should().Be("access-token");
        result.Tokens.RefreshToken.Token.Should().Be("rotated-refresh");
        result.User.Permissions.Should().ContainKey("Projects");
    }

    [Fact]
    public async Task CompleteMfaLoginAsync_InvalidSessionToken_ReturnsNull()
    {
        await using var context = await SeedUserAsync(u => u.MfaEnabled = true);
        _jwt.Setup(j => j.ValidateMfaSessionToken(It.IsAny<string>())).Returns((int?)null);
        var service = CreateService(context);

        var result = await service.CompleteMfaLoginAsync("bad-token", "123456", null, null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CompleteMfaLoginAsync_WrongCode_ReturnsNull_AndAuditsFailure()
    {
        await using var context = await SeedUserAsync(u => u.MfaEnabled = true);
        _jwt.Setup(j => j.ValidateMfaSessionToken("mfa-session")).Returns(1);
        _mfa.Setup(m => m.IsMfaEnabled(It.IsAny<User>())).Returns(true);
        _mfa.Setup(m => m.VerifyTotp(It.IsAny<User>(), "000000")).Returns(false);
        var service = CreateService(context);

        var result = await service.CompleteMfaLoginAsync("mfa-session", "000000", null, null);

        result.Should().BeNull();
        _audit.Verify(a => a.WriteAsync(
            AuthEventType.MfaVerifyFailed, false, 1, "ivanov", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteMfaLoginAsync_ValidCode_ReturnsSuccessWithTokens()
    {
        await using var context = await SeedUserAsync(u => u.MfaEnabled = true);
        _jwt.Setup(j => j.ValidateMfaSessionToken("mfa-session")).Returns(1);
        _mfa.Setup(m => m.IsMfaEnabled(It.IsAny<User>())).Returns(true);
        _mfa.Setup(m => m.VerifyTotp(It.IsAny<User>(), "123456")).Returns(true);
        var service = CreateService(context);

        var result = await service.CompleteMfaLoginAsync("mfa-session", "123456", "127.0.0.1", "agent");

        result.Should().NotBeNull();
        result!.Status.Should().Be(AuthLoginStatus.Success);
        result.Tokens!.AccessToken.Should().Be("access-token");
        var user = await context.Users.SingleAsync(u => u.Id == 1);
        user.LastLoginAt.Should().NotBeNull();
        _audit.Verify(a => a.WriteAsync(
            AuthEventType.MfaVerifySuccess, true, 1, "ivanov", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserInfoAsync_InactiveUser_ReturnsNull()
    {
        await using var context = await SeedUserAsync(u => u.IsActive = false);
        var service = CreateService(context);

        var result = await service.GetUserInfoAsync(1);

        result.Should().BeNull();
    }
}
