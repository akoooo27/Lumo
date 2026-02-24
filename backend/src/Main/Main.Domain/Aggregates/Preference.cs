using System.Diagnostics.CodeAnalysis;

using Main.Domain.Constants;
using Main.Domain.Entities;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Aggregates;

public sealed class Preference : AggregateRoot<PreferenceId>
{
    private readonly List<Instruction> _instructions = [];

    private readonly List<FavoriteModel> _favoriteModels = [];

    public Guid UserId { get; private set; }

    public IReadOnlyCollection<Instruction> Instructions => _instructions.AsReadOnly();

    public IReadOnlyCollection<FavoriteModel> FavoriteModels => _favoriteModels.AsReadOnly();

    public bool MemoryEnabled { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private Preference() { } // For EF Core

    [SetsRequiredMembers]
    private Preference
    (
        PreferenceId id,
        Guid userId,
        DateTimeOffset utcNow
    )
    {
        Id = id;
        UserId = userId;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public static Outcome<Preference> Create
    (
        PreferenceId id,
        Guid userId,
        DateTimeOffset utcNow
    )
    {
        if (userId == Guid.Empty)
            return PreferenceFaults.UserIdRequired;

        Preference preference = new
        (
            id: id,
            userId: userId,
            utcNow: utcNow
        );

        return preference;
    }

    public Outcome<Instruction> AddInstruction
    (
        InstructionId instructionId,
        string content,
        DateTimeOffset utcNow
    )
    {
        ArgumentNullException.ThrowIfNull(content);

        if (_instructions.Count >= PreferenceConstants.MaxInstructionCount)
            return PreferenceFaults.MaxInstructionsReached;

        int priority = _instructions.Count == 0
            ? PreferenceConstants.MinInstructionPriority
            : _instructions.Max(i => i.Priority) + 1;

        Outcome<Instruction> instructionOutcome = Instruction.Create
        (
            id: instructionId,
            preferenceId: Id,
            content: content,
            priority: priority,
            utcNow: utcNow
        );

        if (instructionOutcome.IsFailure)
            return instructionOutcome.Fault;

        Instruction instruction = instructionOutcome.Value;

        _instructions.Add(instruction);
        UpdatedAt = utcNow;

        return instruction;
    }

    public Outcome UpdateInstruction
    (
        InstructionId instructionId,
        string newContent,
        DateTimeOffset utcNow
    )
    {
        ArgumentNullException.ThrowIfNull(newContent);

        Instruction? instruction = _instructions
            .FirstOrDefault(i => i.Id == instructionId);

        if (instruction is null)
            return PreferenceFaults.InstructionNotFound;

        Outcome updateOutcome = instruction.UpdateContent(newContent, utcNow);

        if (updateOutcome.IsFailure)
            return updateOutcome.Fault;

        UpdatedAt = utcNow;

        return Outcome.Success();
    }

    public Outcome RemoveInstruction
    (
        InstructionId instructionId,
        DateTimeOffset utcNow
    )
    {
        Instruction? instruction = _instructions
            .FirstOrDefault(i => i.Id == instructionId);

        if (instruction is null)
            return PreferenceFaults.InstructionNotFound;

        _instructions.Remove(instruction);
        UpdatedAt = utcNow;

        return Outcome.Success();
    }

    public Outcome<FavoriteModel> AddFavoriteModel
    (
        FavoriteModelId favoriteModelId,
        string modelId,
        DateTimeOffset utcNow
    )
    {
        bool alreadyAdded = _favoriteModels
            .Any(fm => fm.ModelId == modelId);

        if (alreadyAdded)
            return PreferenceFaults.AlreadyInFavorites;

        Outcome<FavoriteModel> favoriteModelOutcome = FavoriteModel.Create
        (
            id: favoriteModelId,
            preferenceId: Id,
            modelId: modelId,
            utcNow: utcNow
        );

        if (favoriteModelOutcome.IsFailure)
            return favoriteModelOutcome.Fault;

        FavoriteModel favoriteModel = favoriteModelOutcome.Value;

        _favoriteModels.Add(favoriteModel);
        UpdatedAt = utcNow;

        return favoriteModel;
    }

    public Outcome RemoveFavoriteModel
    (
        FavoriteModelId favoriteModelId,
        DateTimeOffset utcNow
    )
    {
        FavoriteModel? favoriteModel = _favoriteModels
            .FirstOrDefault(fm => fm.Id == favoriteModelId);

        if (favoriteModel is null)
            return PreferenceFaults.ModelNotInFavorites;

        _favoriteModels.Remove(favoriteModel);
        UpdatedAt = utcNow;

        return Outcome.Success();
    }

    public Outcome EnableMemory(DateTimeOffset utcNow)
    {
        if (MemoryEnabled)
            return PreferenceFaults.MemoryAlreadyEnabled;

        MemoryEnabled = true;
        UpdatedAt = utcNow;

        return Outcome.Success();
    }

    public Outcome DisableMemory(DateTimeOffset utcNow)
    {
        if (!MemoryEnabled)
            return PreferenceFaults.MemoryAlreadyDisabled;

        MemoryEnabled = false;
        UpdatedAt = utcNow;

        return Outcome.Success();
    }
}