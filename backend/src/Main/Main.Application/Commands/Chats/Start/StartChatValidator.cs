using FluentValidation;

using Main.Domain.Constants;

namespace Main.Application.Commands.Chats.Start;

internal sealed class StartChatValidator : AbstractValidator<StartChatCommand>
{
    public StartChatValidator()
    {
        RuleFor(scc => scc.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(MessageConstants.MaxContentLength)
            .WithMessage($"Message must not exceed {MessageConstants.MaxContentLength} characters");

        When(scc => scc.ModelId is not null, () =>
        {
            RuleFor(scc => scc.ModelId)
                .NotEmpty().WithMessage("Model ID cannot be empty")
                .MaximumLength(ChatConstants.MaxModelIdLength)
                .WithMessage($"Model ID must not exceed {ChatConstants.MaxModelIdLength} characters");
        });
    }
}