using Contracts.IntegrationEvents.EphemeralChat;

using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Ephemeral;
using Main.Application.Abstractions.Generators;
using Main.Application.Abstractions.Stream;
using Main.Application.Faults;
using Main.Domain.Enums;
using Main.Domain.Models;
using Main.Domain.ReadModels;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.EphemeralChats.SendEphemeralMessage;

internal sealed class SendEphemeralMessageHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IRequestContext requestContext,
    IChatLockService chatLockService,
    IEphemeralChatStore ephemeralChatStore,
    IIdGenerator idGenerator,
    IMessageBus messageBus,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<SendEphemeralMessageCommand, SendEphemeralMessageResponse>
{
    public async ValueTask<Outcome<SendEphemeralMessageResponse>> Handle(SendEphemeralMessageCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        User? user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user is null)
            return UserOperationFaults.NotFound;

        EphemeralChat? ephemeralChat = await ephemeralChatStore.GetAsync(request.EphemeralChatId, cancellationToken);

        if (ephemeralChat is null || ephemeralChat.UserId != userId)
            return EphemeralChatOperationFaults.NotFound;

        bool lockAcquired = await chatLockService.TryAcquireLockAsync
        (
            chatId: ephemeralChat.EphemeralChatId,
            ownerId: requestContext.CorrelationId,
            cancellationToken: cancellationToken
        );

        if (!lockAcquired)
            return EphemeralChatOperationFaults.GenerationInProgress;

        int nextSequenceNumber = ephemeralChat.Messages
            .Select(m => m.SequenceNumber)
            .DefaultIfEmpty(-1)
            .Max() + 1;

        EphemeralMessage ephemeralMessage = new()
        {
            MessageRole = MessageRole.User,
            MessageContent = request.Message,
            SequenceNumber = nextSequenceNumber
        };

        ephemeralChat.Messages.Add(ephemeralMessage);

        try
        {
            StreamId streamId = idGenerator.NewStreamId();

            EphemeralMessageSent ephemeralMessageSent = new()
            {
                EventId = Guid.NewGuid(),
                OccurredAt = dateTimeProvider.UtcNow,
                CorrelationId = Guid.Parse(requestContext.CorrelationId),
                EphemeralChatId = ephemeralChat.EphemeralChatId,
                UserId = user.UserId,
                StreamId = streamId.Value,
                ModelId = ephemeralChat.ModelId,
                Message = request.Message
            };

            await ephemeralChatStore.SaveAsync(ephemeralChat, cancellationToken);
            await messageBus.PublishAsync(ephemeralMessageSent, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            SendEphemeralMessageResponse response = new
            (
                EphemeralChatId: ephemeralChat.EphemeralChatId,
                StreamId: streamId.Value,
                MessageRole: ephemeralMessage.MessageRole.ToString(),
                MessageContent: ephemeralMessage.MessageContent,
                CreatedAt: dateTimeProvider.UtcNow
            );

            return response;
        }
        catch
        {
            await chatLockService.ReleaseLockAsync
            (
                chatId: ephemeralChat.EphemeralChatId,
                ownerId: requestContext.CorrelationId,
                cancellationToken: cancellationToken
            );
            throw;
        }
    }
}