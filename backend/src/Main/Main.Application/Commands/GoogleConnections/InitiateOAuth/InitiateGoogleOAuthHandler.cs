using Main.Application.Abstractions.Google;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Main.Application.Commands.GoogleConnections.InitiateOAuth;

internal sealed class InitiateGoogleOAuthHandler(
    IUserContext userContext,
    IGoogleOAuthClient googleOAuthClient,
    IGoogleOAuthStateStore googleOAuthStateStore)
    : ICommandHandler<InitiateGoogleOAuthCommand, InitiateGoogleOAuthResponse>
{
    public async ValueTask<Outcome<InitiateGoogleOAuthResponse>> Handle(InitiateGoogleOAuthCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        string state = await googleOAuthStateStore.GenerateAndStoreAsync(userId, cancellationToken);

        Uri authorizationUrl = googleOAuthClient.BuildAuthorizationUrl(state);

        InitiateGoogleOAuthResponse response = new(authorizationUrl.ToString());

        return response;
    }
}