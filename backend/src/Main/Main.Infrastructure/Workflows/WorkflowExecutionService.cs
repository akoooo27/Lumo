using System.Globalization;
using System.Text;

using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Workflows;
using Main.Domain.Constants;
using Main.Infrastructure.AI.Search;

using Microsoft.Extensions.Logging;

using OpenAI;
using OpenAI.Chat;

namespace Main.Infrastructure.Workflows;

internal sealed class WorkflowExecutionService(
    OpenAIClient openAiClient,
    IModelRegistry modelRegistry,
    IWebSearchService webSearchService,
    ILogger<WorkflowExecutionService> logger) : IWorkflowExecutionService
{
    private const string SystemPrompt =
        "You are executing a scheduled workflow. Perform the following task and return the result. " +
        "Do not ask clarifying questions. Do not explain what you are doing. Just produce the output.";

    private static readonly TimeSpan ExecutionTimeout = TimeSpan.FromSeconds(60);

    public async Task<WorkflowExecutionResult> ExecuteAsync(
        WorkflowExecutionRequest request,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutCts = new(ExecutionTimeout);
        using CancellationTokenSource linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            string openRouterModelId = modelRegistry.GetOpenRouterModelId(request.ModelId);
            ChatClient chatClient = openAiClient.GetChatClient(openRouterModelId);

            string instruction = request.UseWebSearch
                ? await EnrichWithWebSearchAsync(request.Instruction, linkedCts.Token)
                : request.Instruction;

            ChatCompletionOptions options = new()
            {
                MaxOutputTokenCount = WorkflowConstants.MaxResultTokens
            };

            ChatCompletion result = await chatClient.CompleteChatAsync
            (
                messages:
                [
                    ChatMessage.CreateSystemMessage(SystemPrompt),
                    ChatMessage.CreateUserMessage(instruction)
                ],
                options: options,
                cancellationToken: linkedCts.Token
            );

            string markdown = result.Content[0].Text;

            return new WorkflowExecutionResult
            (
                Success: true,
                ResultMarkdown: markdown,
                FailureMessage: null,
                InputTokens: result.Usage.InputTokenCount,
                OutputTokens: result.Usage.OutputTokenCount,
                TotalTokens: result.Usage.TotalTokenCount
            );
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception,
                "Workflow execution timed out. WorkflowId={WorkflowId}, WorkflowRunId={WorkflowRunId}",
                request.WorkflowId, request.WorkflowRunId);

            return new WorkflowExecutionResult
            (
                Success: false,
                ResultMarkdown: null,
                FailureMessage: "Workflow execution timed out after 60 seconds.",
                InputTokens: 0,
                OutputTokens: 0,
                TotalTokens: 0
            );
        }
        catch (OperationCanceledException)
        {
            throw; // Caller cancellation — propagate
        }
#pragma warning disable CA1031
        catch (Exception exception)
#pragma warning restore CA1031
        {
            logger.LogError(exception,
                "Workflow execution failed. WorkflowId={WorkflowId}, WorkflowRunId={WorkflowRunId}",
                request.WorkflowId, request.WorkflowRunId);

            return new WorkflowExecutionResult
            (
                Success: false,
                ResultMarkdown: null,
                FailureMessage: exception.Message,
                InputTokens: 0,
                OutputTokens: 0,
                TotalTokens: 0
            );
        }
    }

    private async Task<string> EnrichWithWebSearchAsync(string instruction, CancellationToken cancellationToken)
    {
        WebSearchResponse response = await webSearchService
            .SearchAsync(instruction, "general", cancellationToken);

        if (response.Results.Count == 0)
            return instruction;

        StringBuilder sb = new();

        sb.AppendLine("## Web Search Results");
        sb.AppendLine();

        foreach (WebSearchResult result in response.Results)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"**{result.Title}** ({result.Url})");
            sb.AppendLine(result.Content);
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Task");
        sb.AppendLine(instruction);

        return sb.ToString();
    }
}