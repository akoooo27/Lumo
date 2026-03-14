using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Main.Application.Abstractions.Memory;

using Microsoft.SemanticKernel;

namespace Main.Infrastructure.AI.Plugins;

internal sealed class MemoryImporterPlugin(IMemoryStore memoryStore)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public Guid UserId { get; set; }

    public int TotalFound { get; private set; }

    public List<ImportEntry>? ImportEntries { get; private set; }


    [KernelFunction("get_existing_memories")]
    [Description(
        "Retrieve all existing memories for the user. " +
        "Call this first to see what is already stored and how many import slots are available.")]
    public async Task<string> GetExistingMemoriesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<MemoryEntry> memories = await memoryStore.GetRecentAsync
        (
            userId: UserId,
            limit: MemoryConstants.MaxMemoriesPerUser,
            cancellationToken: cancellationToken
        );

        int availableSlots = MemoryConstants.MaxMemoriesPerUser - memories.Count;

        StringBuilder sb = new();
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"Current: {memories.Count}/{MemoryConstants.MaxMemoriesPerUser}. Available slots: {availableSlots}.");

        if (memories.Count > 0)
        {
            sb.AppendLine("Existing memories:");

            foreach (MemoryEntry m in memories)
                sb.AppendLine(CultureInfo.InvariantCulture, $"- [{m.MemoryCategory}] {m.Content}");
        }
        else
        {
            sb.AppendLine("No existing memories. All entries can be imported.");
        }

        return sb.ToString();
    }

    [KernelFunction("submit_import")]
    [Description(
        "Submit the final parsed and deduplicated memories for import. " +
        "Call this ONCE after parsing all entries and removing duplicates.")]
    public string SubmitImport
    (
        [Description("Total number of memory entries found in the raw text, including ones skipped as duplicates")]
        int totalFound,
        [Description
        (
            "JSON array of non-duplicate memories to import. " +
            "Format: [{\"content\":\"...\",\"category\":\"preference\"|\"fact\"|\"instruction\",\"importance\":1-10}]. " +
            "Omit entries that duplicate existing memories.")]
        string memoriesJson
    )
    {
        if (ImportEntries is not null)
            return "Error: submit_import has already been called. Only one submission is allowed per import.";

        TotalFound = totalFound;

        try
        {
            ImportEntries = JsonSerializer.Deserialize<List<ImportEntry>>(memoriesJson, JsonOptions);

            return ImportEntries is null
                ? "Error: Could not parse the memories. Please provide a valid JSON array."
                : string.Format(CultureInfo.InvariantCulture,
                    "Received {0} memories for import out of {1} total found.",
                    ImportEntries.Count, totalFound);
        }
        catch (JsonException)
        {
            ImportEntries = null;
            return "Error: Invalid JSON format. Please provide a valid JSON array.";
        }
    }
}