using FluentValidation;

namespace Main.Application.Commands.Workflows.Trigger;

internal sealed class TriggerWorkflowValidator : AbstractValidator<TriggerWorkflowCommand>
{
    public TriggerWorkflowValidator()
    {
        RuleFor(c => c.WorkflowId)
            .NotEmpty().WithMessage("Workflow ID is required");
    }
}