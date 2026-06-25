namespace EngineeringPerformance.Application.Users;

public interface IUserLookupService
{
    Task<IReadOnlyList<UserLookupDto>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
}
