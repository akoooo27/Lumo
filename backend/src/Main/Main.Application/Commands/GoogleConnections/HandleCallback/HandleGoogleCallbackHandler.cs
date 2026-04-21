using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Generators;
using Main.Application.Abstractions.Google;
using Main.Application.Faults;
using Main.Domain.Aggregates;
using Main.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Messaging;
using SharedKernel.Application.Security;

namespace Main.Application.Commands.GoogleConnections.HandleCallback;

internal sealed class HandleGoogleCallbackHandler(
    IMainDbContext dbContext,
    IGoogleOAuthClient googleOAuthClient,
    IGoogleOAuthStateStore googleOAuthStateStore,
    IIdGenerator idGenerator,
    IDataProtectorWrapper dataProtectorWrapper,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<HandleGoogleCallbackCommand>
{
    public async ValueTask<Outcome> Handle(HandleGoogleCallbackCommand request, CancellationToken cancellationToken)
    {
        Guid? resolvedUserId = await googleOAuthStateStore.ValidateAndConsumeAsync(request.State, cancellationToken);

        if (resolvedUserId is not { } userId)
            return GoogleConnectionOperationFaults.InvalidOAuthState;

        GoogleTokenResponse tokenResponse = await googleOAuthClient.ExchangeCodeAsync(request.Code, cancellationToken);

        string googleEmail = await googleOAuthClient.GetUserEmailAsync(tokenResponse.AccessToken, cancellationToken);

        string protectedAccessToken = dataProtectorWrapper.Protect(tokenResponse.AccessToken);
        string protectedRefreshToken = dataProtectorWrapper.Protect(tokenResponse.RefreshToken);
        DateTimeOffset expiresAt = dateTimeProvider.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds);

        GoogleConnection? googleConnection = await dbContext.GoogleConnections
            .FirstOrDefaultAsync(gc => gc.UserId == userId, cancellationToken);

        if (googleConnection is null)
        {
            GoogleConnectionId googleConnectionId = idGenerator.NewGoogleConnectionId();

            Outcome<GoogleConnection> createOutcome = GoogleConnection.Create
            (
                id: googleConnectionId,
                userId: userId,
                protectedAccessToken: protectedAccessToken,
                protectedRefreshToken: protectedRefreshToken,
                googleEmail: googleEmail,
                utcNow: dateTimeProvider.UtcNow,
                expiresAt: expiresAt
            );

            if (createOutcome.IsFailure)
                return createOutcome.Fault;

            dbContext.GoogleConnections.Add(createOutcome.Value);
        }
        else
        {
            googleConnection.UpdateTokens
            (
                protectedAccessToken: protectedAccessToken,
                protectedRefreshToken: protectedRefreshToken,
                utcNow: dateTimeProvider.UtcNow,
                expiresAt: expiresAt
            );
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Outcome.Success();
    }
}