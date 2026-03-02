using FluentValidation;

namespace Main.Application.Commands.Workflows.Resume;

internal sealed class ResumeWorkflowValidator : AbstractValidator<ResumeWorkflowCommand>
{
    public ResumeWorkflowValidator()
    {
        RuleFor(rwc => rwc.WorkflowId)
            .NotEmpty().WithMessage("Workflow ID is required");
    }
}
