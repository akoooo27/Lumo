using Main.Application.Abstractions.Memory;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Memories.SearchMemories;

internal sealed class SearchMemoriesHandler(IMemoryStore memoryStore, IUserContext userContext)
    : IQueryHandler<SearchMemoriesQuery, SearchMemoriesResponse>
{
    public async ValueTask<Outcome<SearchMemoriesResponse>> Handle(
        SearchMemoriesQuery request,
        CancellationToken cancellationToken)
    {
        string query = request.Query.Trim();

        if (string.IsNullOrWhiteSpace(query))
            return new SearchMemoriesResponse([]);

        int limit = Math.Clamp(request.Limit, 1, MemoryConstants.MaxPageSize);

        IReadOnlyList<MemoryEntry> results = await memoryStore.SearchAsync(
            userId: userContext.UserId,
            query: query,
            limit: limit,
            cancellationToken: cancellationToken);

        SearchMemoriesResponse response = new
        (
            Memories: [.. results.Select(m => new SearchMemoryReadModel
            {
                Id = m.Id,
                Content = m.Content,
                Category = m.MemoryCategory.ToString(),
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                LastAccessedAt = m.LastAccessedAt,
                AccessCount = m.AccessCount,
                Importance = m.Importance
            })]
        );

        return response;
    }
}