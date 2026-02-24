using Main.Application.Abstractions.Memory;
using Main.Application.Faults;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Memories.DeleteSingle;

internal sealed class DeleteMemoryHandler(IMemoryStore memoryStore, IUserContext userContext)
    : ICommandHandler<DeleteMemoryCommand>
{
    public async ValueTask<Outcome> Handle(DeleteMemoryCommand request, CancellationToken cancellationToken)
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

        await memoryStore.SoftDeleteAsync
        (
            userId: userId,
            memoryId: request.MemoryId,
            cancellationToken: cancellationToken
        );

        return Outcome.Success();
    }
}