using FluentValidation;

using Main.Application.Abstractions.Memory;

namespace Main.Application.Commands.Memories.Update;

internal sealed class UpdateMemoryValidator : AbstractValidator<UpdateMemoryCommand>
{
    public UpdateMemoryValidator()
    {
        RuleFor(umc => umc.MemoryId)
            .NotEmpty()
            .WithMessage("Memory ID is required");

        RuleFor(umc => umc)
            .Must(umc => umc.Content is not null || umc.Category is not null || umc.Importance is not null)
            .WithMessage("At least one field (Content, Category, or Importance) must be provided.");

        When(umc => umc.Content is not null, () =>
        {
            RuleFor(umc => umc.Content!)
                .NotEmpty()
                .MaximumLength(MemoryConstants.MaxContentLength);
        });

        When(umc => umc.Category is not null, () =>
        {
            RuleFor(umc => umc.Category!.Value)
                .IsInEnum();
        });

        When(umc => umc.Importance is not null, () =>
        {
            RuleFor(umc => umc.Importance!.Value)
                .InclusiveBetween(MemoryConstants.MinImportance, MemoryConstants.MaxImportance);
        });
    }
}