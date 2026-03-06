using SharedKernel;

namespace Main.Domain.ValueObjects;

public readonly record struct WorkflowId
{
    private const string Prefix = "wfl_";
    private const int TotalLength = 30;

    public string Value { get; }

    private WorkflowId(string value)
    {
        Value = value;
    }

    public static Outcome<WorkflowId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Faults.Required;

        if (!IsValid(value!))
            return Faults.InvalidFormat;

        return new WorkflowId(value);
    }

    public static WorkflowId UnsafeFrom(string value) => new(value);

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
            title: "WorkflowId.Required",
            detail: "WorkflowId is required."
        );

        public static readonly Fault InvalidFormat = Fault.Validation
        (
            title: "WorkflowId.InvalidFormat",
            detail: $"WorkflowId must start with '{Prefix}' and be {TotalLength} characters."
        );
    }
}