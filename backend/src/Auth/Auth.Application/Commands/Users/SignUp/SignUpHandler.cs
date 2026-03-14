using Auth.Application.Abstractions.Authentication;
using Auth.Application.Abstractions.Data;
using Auth.Application.Abstractions.Generators;
using Auth.Application.Abstractions.ZeroBounce;
using Auth.Application.Faults;
using Auth.Domain.Aggregates;
using Auth.Domain.Constants;
using Auth.Domain.ValueObjects;

using Contracts.IntegrationEvents.Auth;

using Microsoft.EntityFrameworkCore;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Auth.Application.Commands.Users.SignUp;

internal sealed class SignUpHandler(
    IAuthDbContext dbContext,
    IRequestContext requestContext,
    IEmailValidationService emailValidationService,
    ISecureTokenGenerator secureTokenGenerator,
    IIdGenerator idGenerator,
    IMessageBus messageBus,
    IDateTimeProvider dateTimeProvider
    ) : ICommandHandler<SignUpCommand, SignUpResponse>
{
    public async ValueTask<Outcome<SignUpResponse>> Handle(SignUpCommand request, CancellationToken cancellationToken)
    {
        Outcome<EmailAddress> emailAddressOutcome = EmailAddress.Create(request.EmailAddress);

        if (emailAddressOutcome.IsFailure)
            return emailAddressOutcome.Fault;

        EmailAddress emailAddress = emailAddressOutcome.Value;

        bool emailExists = await dbContext.Users
            .AnyAsync(u => u.EmailAddress == emailAddress, cancellationToken);

        if (emailExists)
            return UserOperationFaults.EmailAlreadyInUse;

        // Keep the dependency wired for now; spam validation is temporarily disabled.
        _ = emailValidationService;
#pragma warning disable S125
        // try
        // {
        //     bool isSpam = await emailValidationService.IsSpamEmailAsync(emailAddress.Value, cancellationToken);
        //
        //     if (isSpam)
        //         return UserOperationFaults.SpamEmailAddress;
        // }
        // catch (HttpRequestException)
        // {
        //     return UserOperationFaults.EmailValidationUnavailable;
        // }
#pragma warning restore S125

        Outcome<User> userOutcome = User.Create
        (
            displayName: request.DisplayName,
            emailAddress: emailAddress,
            utcNow: dateTimeProvider.UtcNow
        );

        if (userOutcome.IsFailure)
            return userOutcome.Fault;

        User user = userOutcome.Value;

        (List<RecoverKeyInput> recoverKeyInputs, List<string> userFriendlyKeys) = GenerateRecoveryKeys();

        RecoveryKeyChainId recoveryKeyChainId = idGenerator.NewRecoveryKeyChainId();

        Outcome<RecoveryKeyChain> recoveryKeyChainOutcome = RecoveryKeyChain.Create
        (
            id: recoveryKeyChainId,
            userId: user.Id,
            recoverKeyInputs: recoverKeyInputs,
            utcNow: dateTimeProvider.UtcNow
        );

        if (recoveryKeyChainOutcome.IsFailure)
            return recoveryKeyChainOutcome.Fault;

        RecoveryKeyChain recoveryKeyChain = recoveryKeyChainOutcome.Value;

        UserSignedUp userSignedUp = new()
        {
            EventId = Guid.NewGuid(),
            OccurredAt = dateTimeProvider.UtcNow,
            CorrelationId = Guid.Parse(requestContext.CorrelationId),
            UserId = user.Id.Value,
            EmailAddress = user.EmailAddress.Value,
            DisplayName = user.DisplayName
        };

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.RecoveryKeyChains.AddAsync(recoveryKeyChain, cancellationToken);
        await messageBus.PublishAsync(userSignedUp, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        SignUpResponse response = new
        (
            UserFriendlyRecoveryKeys: userFriendlyKeys
        );

        return response;
    }

    private (List<RecoverKeyInput> recoverKeyInputs, List<string> UserFriendlyRecoveryKeys) GenerateRecoveryKeys()
    {
        List<RecoverKeyInput> recoverKeyInputs = new(RecoveryKeyConstants.MaxKeysPerChain);
        List<string> userFriendlyRecoveryKeys = new(RecoveryKeyConstants.MaxKeysPerChain);

        for (int i = 0; i < RecoveryKeyConstants.MaxKeysPerChain; i++)
        {
            string identifier = secureTokenGenerator.GenerateToken(RecoveryKeyConstants.IdentifierLength);
            string verifier = secureTokenGenerator.GenerateToken(RecoveryKeyConstants.VerifierLength);
            string verifierHash = secureTokenGenerator.HashToken(verifier);

            recoverKeyInputs.Add(RecoverKeyInput.Create(identifier, verifierHash));
            userFriendlyRecoveryKeys.Add($"{identifier}.{verifier}");
        }

        return (recoverKeyInputs, userFriendlyRecoveryKeys);
    }
}
