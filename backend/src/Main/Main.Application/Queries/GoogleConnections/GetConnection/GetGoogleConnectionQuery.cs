using SharedKernel.Application.Messaging;

namespace Main.Application.Queries.GoogleConnections.GetConnection;

public sealed record GetGoogleConnectionQuery : IQuery<GetGoogleConnectionResponse>;