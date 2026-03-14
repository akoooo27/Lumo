using FluentValidation;

using Main.Domain.Constants;

namespace Main.Application.Commands.Folders.Create;

internal sealed class CreateFolderValidator : AbstractValidator<CreateFolderCommand>
{
    public CreateFolderValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Folder name is required")
            .MaximumLength(FolderConstants.MaxNameLength)
            .WithMessage($"Folder name must not exceed {FolderConstants.MaxNameLength} characters");
    }
}