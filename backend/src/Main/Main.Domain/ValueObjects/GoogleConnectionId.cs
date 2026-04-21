using SharedKernel;

namespace Main.Domain.ValueObjects;

public readonly record struct GoogleConnectionId
{
    private const string Prefix = "gc_";
    private const int TotalLength = 30;

    public string Value { get; }

    private GoogleConnectionId(string value)
    {
        Value = value;
    }

    public static Outcome<GoogleConnectionId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Faults.Required;

        if (!IsValid(value!))
            return Faults.InvalidFormat;

        return new GoogleConnectionId(value);
    }

    public static GoogleConnectionId UnsafeFrom(string value) => new(value);

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
            title: "GoogleConnectionId.Required",
            detail: "GoogleConnectionId is required."
        );

        public static readonly Fault InvalidFormat = Fault.Validation
        (
            title: "GoogleConnectionId.InvalidFormat",
            detail: $"GoogleConnectionId must start with '{Prefix}' and be {TotalLength} characters."
        );
    }
}