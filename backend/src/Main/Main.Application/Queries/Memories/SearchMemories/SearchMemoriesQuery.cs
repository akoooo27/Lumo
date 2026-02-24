using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Memories.SearchMemories;

public sealed record SearchMemoriesQuery(string Query, int Limit) : IQuery<SearchMemoriesResponse>;