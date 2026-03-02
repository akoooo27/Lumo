namespace Main.Application.Queries.Workflows.GetWorkflowOptions;

public sealed record class WorkflowModelOptionReadModel
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string Provider { get; init; }

    public bool IsDefault { get; init; }

    public bool SupportsFunctionCalling { get; init; }
}
