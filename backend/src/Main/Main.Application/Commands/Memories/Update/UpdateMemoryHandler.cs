using Main.Application.Abstractions.Memory;
using Main.Application.Faults;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Memories.Update;

internal sealed class UpdateMemoryHandler(
    IMemoryStore memoryStore,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateMemoryCommand, UpdateMemoryResponse>
{
    public async ValueTask<Outcome<UpdateMemoryResponse>> Handle(UpdateMemoryCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        MemoryEntry? existing = await memoryStore.GetByIdAsync
        (
            userId: userId,
            memoryId: request.MemoryId,
            cancellationToken: cancellationToken
        );

        if (existing is null)
            return MemoryOperationFaults.NotFound;

        await memoryStore.UpdateAsync
        (
            userId: userId,
            memoryId: request.MemoryId,
            newContent: request.Content,
            cancellationToken: cancellationToken
        );

        UpdateMemoryResponse response = new
        (
            MemoryId: request.MemoryId,
            Content: request.Content,
            UpdatedAt: dateTimeProvider.UtcNow
        );

        return response;
    }
}