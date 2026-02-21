using FluentValidation;

namespace Main.Application.Commands.Memories.DeleteSingle;

internal sealed class DeleteMemoryValidator : AbstractValidator<DeleteMemoryCommand>
{
    public DeleteMemoryValidator()
    {
        RuleFor(dmc => dmc.MemoryId)
            .NotEmpty().WithMessage("Memory ID is required");
    }
}
