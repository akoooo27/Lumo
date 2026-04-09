namespace Main.Infrastructure.AI.Plugins;

internal sealed class PluginStreamContext
{
    public string? StreamId { get; set; }

    public IReadOnlyList<ToolCallSource>? LastSearchSources { get; set; }

    public IReadOnlyList<RecalledMemory>? RecalledMemories { get; set; }

    public string? SourcesJson { get; set; }
}