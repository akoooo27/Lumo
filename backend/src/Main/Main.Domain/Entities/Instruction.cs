using System.Diagnostics.CodeAnalysis;

using Main.Domain.Constants;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Entities;

public sealed class Instruction : Entity<InstructionId>
{
    public PreferenceId PreferenceId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public int Priority { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private Instruction() { } // For EF Core

    [SetsRequiredMembers]
    private Instruction
    (
        InstructionId id,
        PreferenceId preferenceId,
        string content,
        int priority,
        DateTimeOffset utcNow
    )
    {
        Id = id;
        PreferenceId = preferenceId;
        Content = content;
        Priority = priority;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    internal static Outcome<Instruction> Create
    (
        InstructionId id,
        PreferenceId preferenceId,
        string content,
        int priority,
        DateTimeOffset utcNow
    )
    {
        if (id.IsEmpty)
            return InstructionFaults.InstructionIdRequired;

        if (preferenceId.IsEmpty)
            return InstructionFaults.PreferenceIdRequired;

        Outcome validationOutcome = ValidateContent(content);

        if (!validationOutcome.IsSuccess)
            return validationOutcome.Fault;

        Instruction instruction = new Instruction
        (
            id: id,
            preferenceId: preferenceId,
            content: content.Trim(),
            priority: priority,
            utcNow: utcNow
        );

        return instruction;
    }

    internal Outcome UpdateContent(string newContent, DateTimeOffset utcNow)
    {
        Outcome validationOutcome = ValidateContent(newContent);

        if (!validationOutcome.IsSuccess)
            return validationOutcome.Fault;

        Content = newContent.Trim();
        UpdatedAt = utcNow;

        return Outcome.Success();
    }

    internal void UpdatePriority(int newPriority, DateTimeOffset utcNow)
    {
        Priority = newPriority;
        UpdatedAt = utcNow;
    }

    private static Outcome ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Outcome.Failure(InstructionFaults.ContentEmpty);

        string trimmedContent = content.Trim();

        if (trimmedContent.Length < InstructionConstants.MinContentLength)
            return Outcome.Failure(InstructionFaults.ContentTooShort);

        if (trimmedContent.Length > InstructionConstants.MaxContentLength)
            return Outcome.Failure(InstructionFaults.ContentTooLong);

        return Outcome.Success();
    }
}