using FluentValidation;

using Main.Application.Abstractions.Memory;

namespace Main.Application.Commands.Memories.Update;

internal sealed class UpdateMemoryValidator : AbstractValidator<UpdateMemoryCommand>
{
    public UpdateMemoryValidator()
    {
        RuleFor(umc => umc.MemoryId)
            .NotEmpty().WithMessage("Memory ID is required");

        RuleFor(umc => umc.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(MemoryConstants.MaxContentLength)
            .WithMessage($"Content must not exceed {MemoryConstants.MaxContentLength} characters");
    }
}