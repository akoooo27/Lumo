namespace Main.Application.Abstractions.Google;

public interface IGoogleOAuthStateStore
{
    Task<string> GenerateAndStoreAsync(Guid userId, CancellationToken cancellationToken);

    Task<Guid?> ValidateAndConsumeAsync(string state, CancellationToken cancellationToken);
}