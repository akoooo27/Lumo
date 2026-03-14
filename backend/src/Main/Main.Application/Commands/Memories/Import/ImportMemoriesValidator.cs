using FluentValidation;

using Main.Application.Abstractions.Memory;

namespace Main.Application.Commands.Memories.Import;

internal sealed class ImportMemoriesValidator : AbstractValidator<ImportMemoriesCommand>
{
    public ImportMemoriesValidator()
    {
        RuleFor(imc => imc.Content)
            .NotEmpty()
            .WithMessage("Import content is required.")
            .MaximumLength(MemoryConstants.MaxImportContentLength)
            .WithMessage("Import content is too large.");
    }
}