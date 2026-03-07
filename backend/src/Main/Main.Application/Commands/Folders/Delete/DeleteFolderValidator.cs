using FluentValidation;

namespace Main.Application.Commands.Folders.Delete;

internal sealed class DeleteFolderValidator : AbstractValidator<DeleteFolderCommand>
{
    public DeleteFolderValidator()
    {
        RuleFor(c => c.FolderId)
            .NotEmpty().WithMessage("Folder ID is required");
    }
}