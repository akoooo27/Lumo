using FluentValidation;

namespace Main.Application.Commands.EphemeralChats.StopGeneration;

internal sealed class StopEphemeralGenerationValidator : AbstractValidator<StopEphemeralGenerationCommand>
{
    public StopEphemeralGenerationValidator()
    {
        RuleFor(segc => segc.EphemeralChatId)
            .NotEmpty().WithMessage("Ephemeral Chat ID is required");

        RuleFor(segc => segc.StreamId)
            .NotEmpty().WithMessage("Stream ID is required");
    }
}