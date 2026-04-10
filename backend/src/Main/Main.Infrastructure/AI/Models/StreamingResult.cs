using Main.Application.Abstractions.AI;

namespace Main.Infrastructure.AI.Models;

public sealed record StreamingResult(TokenUsage TokenUsage, bool WasCancelled);