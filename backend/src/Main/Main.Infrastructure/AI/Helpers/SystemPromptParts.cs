namespace Main.Infrastructure.AI.Helpers;

internal sealed record SystemPromptParts
(
    string Core,
    string? UserInstructions,
    string? ToolGuidance
);