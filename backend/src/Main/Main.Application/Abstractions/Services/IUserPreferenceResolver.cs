namespace Main.Application.Abstractions.Services;

public interface IUserPreferenceResolver
{
    Task<bool> IsMemoryEnabledAsync(Guid userId, CancellationToken cancellationToken);
}