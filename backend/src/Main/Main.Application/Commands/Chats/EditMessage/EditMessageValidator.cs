using FluentValidation;

using Main.Domain.Constants;

namespace Main.Application.Commands.Chats.EditMessage;

internal sealed class EditMessageValidator : AbstractValidator<EditMessageCommand>
{
    public EditMessageValidator()
    {
        RuleFor(emc => emc.ChatId)
            .NotEmpty().WithMessage("Chat ID is required");

        RuleFor(emc => emc.MessageId)
            .NotEmpty().WithMessage("Message ID is required");

        RuleFor(emc => emc.NewContent)
            .NotEmpty().WithMessage("New content is required")
            .MaximumLength(MessageConstants.MaxContentLength)
            .WithMessage($"New content must not exceed {MessageConstants.MaxContentLength} characters");
    }
}