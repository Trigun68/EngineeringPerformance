using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Auth;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using OtpNet;

namespace EngineeringPerformance.Infrastructure.Tests.Auth;

public sealed class MfaServiceTests
{
    [Fact]
    public async Task VerifyTotp_AfterEnable_AcceptsCodeForKnownSecret()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new AppDbContext(options);
        var user = new User { Username = "mfa-user", Role = new Role { Name = "Admin" } };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new MfaService(
            context,
            DataProtectionProvider.Create("mfa-service-tests"),
            Options.Create(new MfaOptions()),
            new Mock<IUserActionLogger>().Object);
        const string secret = "JBSWY3DPEHPK3PXP";
        var code = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp(DateTime.UtcNow);

        await service.EnableMfaAsync(user, secret);

        user.MfaSecret.Should().NotBe(secret);
        service.VerifyTotp(user, code).Should().BeTrue();
    }
}
