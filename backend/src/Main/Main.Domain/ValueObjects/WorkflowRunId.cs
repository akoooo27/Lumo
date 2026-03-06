using SharedKernel;

namespace Main.Domain.ValueObjects;

public readonly record struct WorkflowRunId
{
    private const string Prefix = "wfr_";
    private const int TotalLength = 30;

    public string Value { get; }

    private WorkflowRunId(string value)
    {
        Value = value;
    }

    public static Outcome<WorkflowRunId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Faults.Required;

        if (!IsValid(value!))
            return Faults.InvalidFormat;

        return new WorkflowRunId(value);
    }

    public static WorkflowRunId UnsafeFrom(string value) => new(value);

    public static string PrefixValue => Prefix;

    public static int Length => TotalLength;

    private static bool IsValid(string value) =>
        value.Length == TotalLength && value.StartsWith(Prefix, StringComparison.Ordinal);

    public override string ToString() => Value;

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    private static class Faults
    {
        public static readonly Fault Required = Fault.Validation
        (
            title: "WorkflowRunId.Required",
            detail: "WorkflowRunId is required."
        );

        public static readonly Fault InvalidFormat = Fault.Validation
        (
            title: "WorkflowRunId.InvalidFormat",
            detail: $"WorkflowRunId must start with '{Prefix}' and be {TotalLength} characters."
        );
    }
}