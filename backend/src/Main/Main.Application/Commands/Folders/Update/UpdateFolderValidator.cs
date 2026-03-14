using FluentValidation;

using Main.Domain.Constants;

namespace Main.Application.Commands.Folders.Update;

internal sealed class UpdateFolderValidator : AbstractValidator<UpdateFolderCommand>
{
    public UpdateFolderValidator()
    {
        RuleFor(c => c.FolderId)
            .NotEmpty().WithMessage("Folder ID is required");

        When(c => c.NewName is not null, () =>
        {
            RuleFor(c => c.NewName)
                .NotEmpty().WithMessage("Folder name cannot be empty")
                .MaximumLength(FolderConstants.MaxNameLength)
                .WithMessage($"Folder name must not exceed {FolderConstants.MaxNameLength} characters");
        });
    }
}