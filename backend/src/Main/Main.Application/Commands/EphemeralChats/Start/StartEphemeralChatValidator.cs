using FluentValidation;

using Main.Domain.Constants;

namespace Main.Application.Commands.EphemeralChats.Start;

internal sealed class StartEphemeralChatValidator : AbstractValidator<StartEphemeralChatCommand>
{
    public StartEphemeralChatValidator()
    {
        RuleFor(cmd => cmd.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(MessageConstants.MaxContentLength)
            .WithMessage($"Message must not exceed {MessageConstants.MaxContentLength} characters");

        When(cmd => cmd.ModelId is not null, () =>
        {
            RuleFor(cmd => cmd.ModelId)
                .NotEmpty().WithMessage("Model ID cannot be empty")
                .MaximumLength(ChatConstants.MaxModelIdLength)
                .WithMessage($"Model ID must not exceed {ChatConstants.MaxModelIdLength} characters");
        });
    }
}