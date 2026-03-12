using Main.Application.Abstractions.Memory;
using Main.Application.Faults;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Memories.Import;

internal sealed class ImportMemoriesHandler(
    IUserContext userContext,
    IMemoryImportService memoryImportService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ImportMemoriesCommand, ImportMemoriesResponse>
{
    public async ValueTask<Outcome<ImportMemoriesResponse>> Handle(ImportMemoriesCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        MemoryImportResult? memoryImportResult = await memoryImportService.ImportAsync
        (
            userId: userId,
            rawText: request.Content,
            cancellationToken: cancellationToken
        );

        if (memoryImportResult is null)
            return MemoryOperationFaults.NoMemoriesToImport;

        ImportMemoriesResponse response = new
        (
            Imported: memoryImportResult.Imported,
            SkippedAsDuplicates: memoryImportResult.SkippedAsDuplicates,
            SkippedDueToCapacity: memoryImportResult.SkippedDueToCapacity,
            Total: memoryImportResult.Total,
            ImportedAt: dateTimeProvider.UtcNow
        );

        return response;
    }
}