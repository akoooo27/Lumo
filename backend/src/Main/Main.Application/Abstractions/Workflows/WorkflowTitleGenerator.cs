using Main.Domain.Constants;

namespace Main.Application.Abstractions.Workflows;

internal static class WorkflowTitleGenerator
{
    public static string Generate(string instruction)
    {
        const int maxWords = 8;
        string trimmed = instruction.Trim();

        string[] words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        string title = words.Length <= maxWords
            ? trimmed
            : string.Join(' ', words.Take(maxWords)) + "...";

        return title.Length > WorkflowConstants.MaxTitleLength
            ? title[..WorkflowConstants.MaxTitleLength]
            : title;
    }
}