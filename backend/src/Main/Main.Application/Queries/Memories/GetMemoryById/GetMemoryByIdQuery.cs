using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.Memories.GetMemoryById;

public sealed record GetMemoryByIdQuery(string MemoryId) : IQuery<GetMemoryByIdResponse>;