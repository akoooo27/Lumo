using FluentValidation;

using Main.Domain.Constants;

namespace Main.Application.Commands.Chats.SendMessage;

internal sealed class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(smc => smc.ChatId)
            .NotEmpty().WithMessage("Chat ID is required");

        RuleFor(smc => smc.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(MessageConstants.MaxContentLength)
            .WithMessage($"Message must not exceed {MessageConstants.MaxContentLength} characters");
    }
}