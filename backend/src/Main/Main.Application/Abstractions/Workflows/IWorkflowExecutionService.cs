namespace Main.Application.Abstractions.Workflows;

public interface IWorkflowExecutionService
{
    Task<WorkflowExecutionResult> ExecuteAsync(WorkflowExecutionRequest request, CancellationToken cancellationToken);
}