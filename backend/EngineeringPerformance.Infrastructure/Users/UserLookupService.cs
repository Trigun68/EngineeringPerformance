using EngineeringPerformance.Application.Users;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Users;

public sealed class UserLookupService : IUserLookupService
{
    private readonly AppDbContext _context;

    public UserLookupService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserLookupDto>> GetActiveUsersAsync(CancellationToken cancellationToken = default) =>
        await _context.Users.AsNoTracking()
            .Where(u => u.IsActive && !u.IsArchived)
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserLookupDto(u.Id, u.Username, u.DisplayName, u.Role.Name))
            .ToListAsync(cancellationToken);
}
