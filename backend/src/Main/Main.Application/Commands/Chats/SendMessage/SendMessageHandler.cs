using Contracts.IntegrationEvents.Chat;

using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Generators;
using Main.Application.Abstractions.Storage;
using Main.Application.Abstractions.Stream;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.Entities;
using Main.Domain.ReadModels;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Chats.SendMessage;

internal sealed class SendMessageHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IRequestContext requestContext,
    IModelRegistry modelRegistry,
    IChatLockService chatLockService,
    IIdGenerator idGenerator,
    IStorageService storageService,
    IMessageBus messageBus,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<SendMessageCommand, SendMessageResponse>
{
    public async ValueTask<Outcome<SendMessageResponse>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        User? user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user is null)
            return UserOperationFaults.NotFound;

        Outcome<ChatId> chatIdOutcome = ChatId.From(request.ChatId);

        if (chatIdOutcome.IsFailure)
            return chatIdOutcome.Fault;

        ChatId chatId = chatIdOutcome.Value;

        Chat? chat = await dbContext.Chats
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId, cancellationToken);

        if (chat is null)
            return ChatOperationFaults.NotFound;

        if (request.AttachmentDto is not null)
        {
            ModelInfo? modelInfo = modelRegistry.GetModelInfo(chat.ModelId);

            if (modelInfo is null || !modelInfo.ModelCapabilities.SupportsVision)
                return ChatOperationFaults.AttachmentsNotSupported;
        }

        bool lockAcquired = await chatLockService.TryAcquireLockAsync
        (
            chatId: chat.Id.Value,
            ownerId: requestContext.CorrelationId,
            cancellationToken: cancellationToken
        );

        if (!lockAcquired)
            return ChatOperationFaults.GenerationInProgress;

        MessageId messageId = idGenerator.NewMessageId();

        Outcome<Message> messageOutcome = chat.AddUserMessage
        (
            messageId: messageId,
            messageContent: request.Message,
            utcNow: dateTimeProvider.UtcNow
        );

        if (messageOutcome.IsFailure)
        {
            await chatLockService.ReleaseLockAsync
            (
                chatId: chat.Id.Value,
                ownerId: requestContext.CorrelationId,
                cancellationToken
            );
            return messageOutcome.Fault;
        }

        Message message = messageOutcome.Value;

        if (request.AttachmentDto is not null)
        {
            bool fileExists = await storageService.FileExistsAsync
            (
                fileKey: request.AttachmentDto.FileKey,
                cancellationToken: cancellationToken
            );

            if (!fileExists)
            {
                await chatLockService.ReleaseLockAsync
                (
                    chatId: chat.Id.Value,
                    ownerId: requestContext.CorrelationId,
                    cancellationToken
                );
                return AttachmentOperationFault.NotFound;
            }

            Outcome<Attachment> attachmentOutcome = Attachment.Create
            (
                fileKey: request.AttachmentDto.FileKey,
                contentType: request.AttachmentDto.ContentType,
                fileSizeInBytes: request.AttachmentDto.FileSizeInBytes
            );

            if (attachmentOutcome.IsFailure)
            {
                await chatLockService.ReleaseLockAsync
                (
                    chatId: chat.Id.Value,
                    ownerId: requestContext.CorrelationId,
                    cancellationToken
                );
                return attachmentOutcome.Fault;
            }

            Attachment attachment = attachmentOutcome.Value;

            Outcome setOutcome = chat.SetMessageAttachment
            (
                messageId: message.Id,
                attachment: attachment
            );

            if (setOutcome.IsFailure)
            {
                await chatLockService.ReleaseLockAsync
                (
                    chatId: chat.Id.Value,
                    ownerId: requestContext.CorrelationId,
                    cancellationToken
                );
                return setOutcome.Fault;
            }
        }

        try
        {
            StreamId streamId = idGenerator.NewStreamId();

            MessageSent messageSent = new()
            {
                EventId = Guid.NewGuid(),
                OccurredAt = dateTimeProvider.UtcNow,
                CorrelationId = Guid.Parse(requestContext.CorrelationId),
                ChatId = chat.Id.Value,
                UserId = user.UserId,
                StreamId = streamId.Value,
                ModelId = chat.ModelId,
                Message = request.Message,
                WebSearchEnabled = request.WebSearchEnabled
            };

            await messageBus.PublishAsync(messageSent, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            SendMessageResponse response = new
            (
                MessageId: message.Id.Value,
                ChatId: chat.Id.Value,
                StreamId: streamId.Value,
                MessageRole: message.MessageRole.ToString(),
                MessageContent: message.MessageContent,
                CreatedAt: message.CreatedAt
            );

            return response;
        }
        catch
        {
            await chatLockService.ReleaseLockAsync
            (
                chatId: chat.Id.Value,
                ownerId: requestContext.CorrelationId,
                cancellationToken
            );
            throw;
        }
    }
}