using SharedKernel;

namespace Main.Domain.ValueObjects;

public readonly record struct FolderId
{
    private const string Prefix = "fld_";
    private const int TotalLength = 30;

    public string Value { get; }

    private FolderId(string value)
    {
        Value = value;
    }

    public static Outcome<FolderId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Faults.Required;

        if (!IsValid(value!))
            return Faults.InvalidFormat;

        return new FolderId(value);
    }

    public static FolderId UnsafeFrom(string value) => new(value);

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
            title: "FolderId.Required",
            detail: "FolderId is required."
        );

        public static readonly Fault InvalidFormat = Fault.Validation
        (
            title: "FolderId.InvalidFormat",
            detail: $"FolderId must start with '{Prefix}' and be {TotalLength} characters."
        );
    }
}