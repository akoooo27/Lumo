using FluentValidation;

namespace Main.Application.Commands.Workflows.Pause;

internal sealed class PauseWorkflowValidator : AbstractValidator<PauseWorkflowCommand>
{
    public PauseWorkflowValidator()
    {
        RuleFor(pwc => pwc.WorkflowId)
            .NotEmpty().WithMessage("Workflow ID is required");
    }
}
