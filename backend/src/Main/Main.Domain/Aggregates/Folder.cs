using System.Diagnostics.CodeAnalysis;

using Main.Domain.Constants;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Aggregates;

public sealed class Folder : AggregateRoot<FolderId>
{
    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    private Folder() { } // For EF Core

    [SetsRequiredMembers]
    private Folder
    (
        FolderId id,
        Guid userId,
        string name,
        string normalizedName,
        int sortOrder,
        DateTimeOffset utcNow
    )
    {
        Id = id;
        UserId = userId;
        Name = name;
        NormalizedName = normalizedName;
        SortOrder = sortOrder;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public static Outcome<Folder> Create
    (
        FolderId id,
        Guid userId,
        string name,
        int sortOrder,
        DateTimeOffset utcNow
    )
    {
        if (userId == Guid.Empty)
            return FolderFaults.UserIdRequired;

        Outcome nameOutcome = ValidateName(name);

        if (nameOutcome.IsFailure)
            return nameOutcome.Fault;

        if (sortOrder < 0)
            return FolderFaults.InvalidSortOrder;

        Folder folder = new
        (
            id: id,
            userId: userId,
            name: name.Trim(),
            normalizedName: NormalizeName(name),
            sortOrder: sortOrder,
            utcNow: utcNow
        );

        return folder;
    }

    public Outcome Rename(string newName, DateTimeOffset utcNow)
    {
        Outcome nameOutcome = ValidateName(newName);

        if (nameOutcome.IsFailure)
            return nameOutcome.Fault;

        Name = newName.Trim();
        NormalizedName = NormalizeName(newName);
        UpdatedAt = utcNow;

        return Outcome.Success();
    }

    public Outcome SetSortOrder(int sortOrder, DateTimeOffset utcNow)
    {
        if (sortOrder < 0)
            return FolderFaults.InvalidSortOrder;

        SortOrder = sortOrder;
        UpdatedAt = utcNow;

        return Outcome.Success();
    }

    private static Outcome ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return FolderFaults.NameRequired;

        if (name.Length > FolderConstants.MaxNameLength)
            return FolderFaults.NameTooLong;

        return Outcome.Success();
    }

    internal static string NormalizeName(string name) =>
        name.Trim().ToUpperInvariant();
}