using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Google;
using Main.Application.Faults;
using Main.Domain.Aggregates;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;
using SharedKernel.Application.Security;

namespace Main.Application.Commands.GoogleConnections.Revoke;

internal sealed class RevokeGoogleConnectionHandler(
    IMainDbContext dbContext,
    IUserContext userContext,
    IGoogleOAuthClient googleOAuthClient,
    IDataProtectorWrapper dataProtectorWrapper) : ICommandHandler<RevokeGoogleConnectionCommand>
{
    public async ValueTask<Outcome> Handle(RevokeGoogleConnectionCommand request, CancellationToken cancellationToken)
    {
        GoogleConnection? googleConnection = await dbContext.GoogleConnections
            .FirstOrDefaultAsync(gc => gc.UserId == userContext.UserId, cancellationToken);

        if (googleConnection is null)
            return GoogleConnectionOperationFaults.ConnectionNotFound;

        string refreshToken = dataProtectorWrapper.Unprotect(googleConnection.ProtectedRefreshToken);

        await googleOAuthClient.RevokeTokenAsync(refreshToken, cancellationToken);

        dbContext.GoogleConnections.Remove(googleConnection);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Outcome.Success();
    }
}