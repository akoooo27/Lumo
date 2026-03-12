namespace Main.Application.Abstractions.Stream;

public interface IStreamPublisher
{
    Task PublishStatusAsync
    (
        string streamId,
        StreamStatus status,
        CancellationToken cancellationToken,
        string? fault = null,
        string? modelName = null,
        string? provider = null
    );

    Task SetStreamExpirationAsync(string streamId, TimeSpan expiration, CancellationToken cancellationToken);

    Task PublishChunkAsync(string streamId, string messageContent, CancellationToken cancellationToken);

    Task PublishToolCallAsync(string streamId, string toolName, string? query, CancellationToken cancellationToken);

    Task PublishToolCallResultAsync(string streamId, string toolName, string sourcesJson, CancellationToken cancellationToken);
}