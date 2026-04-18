namespace Main.Application.Abstractions.Google;

public interface IGoogleTokenProvider
{
    Task<string?> GetAccessTokenAsync(Guid userId, CancellationToken cancellationToken);
}