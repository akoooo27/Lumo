using FluentValidation;

namespace Main.Application.Commands.Chats.StopGeneration;

internal sealed class StopGenerationValidator : AbstractValidator<StopGenerationCommand>
{
    public StopGenerationValidator()
    {
        RuleFor(sgc => sgc.ChatId)
            .NotEmpty().WithMessage("Chat ID is required");

        RuleFor(sgc => sgc.StreamId)
            .NotEmpty().WithMessage("Stream ID is required");
    }
}