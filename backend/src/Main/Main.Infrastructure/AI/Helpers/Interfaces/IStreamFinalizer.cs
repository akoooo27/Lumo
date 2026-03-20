using System.Text;

using Main.Application.Abstractions.AI;

namespace Main.Infrastructure.AI.Helpers.Interfaces;

public interface IStreamFinalizer
{
    Task FinalizeAsync
    (
        string streamId,
        string chatId,
        string modelId,
        StringBuilder messageContent,
        TokenUsage tokenUsage,
        bool isAdvanced,
        CancellationToken cancellationToken
    );
}