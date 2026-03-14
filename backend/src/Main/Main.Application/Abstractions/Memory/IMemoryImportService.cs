namespace Main.Application.Abstractions.Memory;

public interface IMemoryImportService
{
    Task<MemoryImportResult?> ImportAsync(Guid userId, string rawText, CancellationToken cancellationToken);
}