using Main.Application.Abstractions.Data;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.Workflows.Delete;

internal sealed class DeleteWorkflowHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeleteWorkflowCommand>
{
    public async ValueTask<Outcome> Handle(DeleteWorkflowCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        Outcome<WorkflowId> workflowIdOutcome = WorkflowId.From(request.WorkflowId);

        if (workflowIdOutcome.IsFailure)
            return workflowIdOutcome.Fault;

        WorkflowId workflowId = workflowIdOutcome.Value;

        Workflow? workflow = await dbContext.Workflows
            .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

        if (workflow is null)
            return WorkflowFaults.NotFound;

        if (workflow.UserId != userId)
            return WorkflowOperationFaults.NotOwner;

        workflow.Archive(dateTimeProvider.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Outcome.Success();
    }
}
