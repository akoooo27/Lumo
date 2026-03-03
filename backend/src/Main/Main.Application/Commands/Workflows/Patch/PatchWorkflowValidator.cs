using FluentValidation;

using Main.Domain.Enums;

namespace Main.Application.Commands.Workflows.Patch;

internal sealed class PatchWorkflowValidator : AbstractValidator<PatchWorkflowCommand>
{
    public PatchWorkflowValidator()
    {
        RuleFor(c => c.WorkflowId)
            .NotEmpty().WithMessage("Workflow ID is required.");

        When(c => c.Status is not null, () =>
        {
            RuleFor(c => c.Status)
                .Must(s => s is WorkflowStatus.Active or WorkflowStatus.Paused or WorkflowStatus.Archived)
                .WithMessage("Status must be Active, Paused, or Archived.");
        });
    }
}