using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class SpnegoIdentityResolver(AppDbContext context) : ISpnegoIdentityResolver
{
    public async Task<SpnegoResolvedUser?> ResolveAsync(string remoteUser, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(remoteUser))
            return null;

        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(
                u => u.IsActive && (u.WindowsUsername == remoteUser || u.Username == remoteUser),
                cancellationToken);

        if (user is null)
            return null;

        return new SpnegoResolvedUser(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Role.Name,
            user.RoleId);
    }
}
