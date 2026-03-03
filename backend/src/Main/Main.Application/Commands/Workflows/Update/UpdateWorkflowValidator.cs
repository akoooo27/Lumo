using FluentValidation;

using Main.Domain.Constants;

namespace Main.Application.Commands.Workflows.Update;

internal sealed class UpdateWorkflowValidator : AbstractValidator<UpdateWorkflowCommand>
{
    public UpdateWorkflowValidator()
    {
        RuleFor(uwc => uwc.WorkflowId)
            .NotEmpty().WithMessage("Workflow ID is required");

        RuleFor(uwc => uwc.Instruction)
            .NotEmpty().WithMessage("Instruction is required")
            .MaximumLength(WorkflowConstants.MaxInstructionLength)
            .WithMessage($"Instruction must not exceed {WorkflowConstants.MaxInstructionLength} characters");

        RuleFor(uwc => uwc.ModelId)
            .NotEmpty().WithMessage("Model ID is required")
            .MaximumLength(WorkflowConstants.MaxModelIdLength)
            .WithMessage($"Model ID must not exceed {WorkflowConstants.MaxModelIdLength} characters");

        RuleFor(uwc => uwc.LocalTime)
            .NotEmpty().WithMessage("Local time is required")
            .Matches(@"^\d{2}:\d{2}$").WithMessage("Local time must be in HH:mm format");

        RuleFor(uwc => uwc.TimeZoneId)
            .NotEmpty().WithMessage("Timezone is required")
            .MaximumLength(WorkflowConstants.MaxTimeZoneIdLength)
            .WithMessage($"Timezone must not exceed {WorkflowConstants.MaxTimeZoneIdLength} characters");

        When(uwc => uwc.Title is not null, () =>
        {
            RuleFor(uwc => uwc.Title)
                .MaximumLength(WorkflowConstants.MaxTitleLength)
                .WithMessage($"Title must not exceed {WorkflowConstants.MaxTitleLength} characters");
        });
    }
}