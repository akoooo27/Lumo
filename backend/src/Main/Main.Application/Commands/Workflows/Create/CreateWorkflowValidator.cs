using FluentValidation;

using Main.Domain.Constants;

namespace Main.Application.Commands.Workflows.Create;

internal sealed class CreateWorkflowValidator : AbstractValidator<CreateWorkflowCommand>
{
    public CreateWorkflowValidator()
    {
        RuleFor(cwc => cwc.Instruction)
            .NotEmpty().WithMessage("Instruction is required")
            .MaximumLength(WorkflowConstants.MaxInstructionLength)
            .WithMessage($"Instruction must not exceed {WorkflowConstants.MaxInstructionLength} characters");

        RuleFor(cwc => cwc.ModelId)
            .NotEmpty().WithMessage("Model ID is required")
            .MaximumLength(WorkflowConstants.MaxModelIdLength)
            .WithMessage($"Model ID must not exceed {WorkflowConstants.MaxModelIdLength} characters");

        RuleFor(cwc => cwc.LocalTime)
            .NotEmpty().WithMessage("Local time is required")
            .Matches(@"^\d{2}:\d{2}$").WithMessage("Local time must be in HH:mm format");

        RuleFor(cwc => cwc.TimeZoneId)
            .NotEmpty().WithMessage("Timezone is required")
            .MaximumLength(WorkflowConstants.MaxTimeZoneIdLength)
            .WithMessage($"Timezone must not exceed {WorkflowConstants.MaxTimeZoneIdLength} characters");

        When(cwc => cwc.Title is not null, () =>
        {
            RuleFor(cwc => cwc.Title)
                .MaximumLength(WorkflowConstants.MaxTitleLength)
                .WithMessage($"Title must not exceed {WorkflowConstants.MaxTitleLength} characters");
        });
    }
}
