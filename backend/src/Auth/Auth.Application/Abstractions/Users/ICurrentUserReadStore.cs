using Auth.Application.Queries.Users.GetCurrentUser;

namespace Auth.Application.Abstractions.Users;

public interface ICurrentUserReadStore
{
    Task<UserReadModel?> GetAsync(Guid userId, CancellationToken cancellationToken);

    Task InvalidateCacheAsync(Guid userId, CancellationToken cancellationToken);
}