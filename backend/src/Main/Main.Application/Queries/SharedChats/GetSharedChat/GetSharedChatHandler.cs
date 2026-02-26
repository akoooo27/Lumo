using Main.Application.Abstractions.SharedChats;
using Main.Application.Faults;
using Main.Domain.ValueObjects;

using Mediator;

using SharedKernel;

namespace Main.Application.Queries.SharedChats.GetSharedChat;

internal sealed class GetSharedChatHandler(ISharedChatReadStore sharedChatReadStore, IPublisher publisher)
    : SharedKernel.Application.Messaging.IQueryHandler<GetSharedChatQuery, GetSharedChatResponse>
{
    public async ValueTask<Outcome<GetSharedChatResponse>> Handle(GetSharedChatQuery request,
        CancellationToken cancellationToken)
    {
        Outcome<SharedChatId> sharedChatIdOutcome = SharedChatId.From(request.SharedChatId);

        if (sharedChatIdOutcome.IsFailure)
            return sharedChatIdOutcome.Fault;

        SharedChatId sharedChatId = sharedChatIdOutcome.Value;

        GetSharedChatResponse? response = await sharedChatReadStore.GetAsync(sharedChatId.Value, cancellationToken);

        if (response is null)
            return SharedChatOperationFaults.NotFound;

        await publisher.Publish(new SharedChatViewedNotification(request.SharedChatId), cancellationToken);

        return response;
    }
}