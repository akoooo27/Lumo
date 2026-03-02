using FluentValidation;

namespace Main.Application.Commands.Workflows.Delete;

internal sealed class DeleteWorkflowValidator : AbstractValidator<DeleteWorkflowCommand>
{
    public DeleteWorkflowValidator()
    {
        RuleFor(dwc => dwc.WorkflowId)
            .NotEmpty().WithMessage("Workflow ID is required");
    }
}
