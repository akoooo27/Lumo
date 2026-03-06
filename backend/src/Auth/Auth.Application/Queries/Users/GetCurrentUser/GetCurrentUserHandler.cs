using Auth.Application.Abstractions.Users;
using Auth.Application.Faults;
using Auth.Domain.ValueObjects;

using SharedKernel;
using SharedKernel.Application.Authentication;
using SharedKernel.Application.Messaging;

namespace Auth.Application.Queries.Users.GetCurrentUser;

internal sealed class GetCurrentUserHandler(IUserContext userContext, ICurrentUserReadStore currentUserReadStore)
    : IQueryHandler<GetCurrentUserQuery, UserReadModel>
{
    public async ValueTask<Outcome<UserReadModel>> Handle(GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        Outcome<UserId> userIdOutcome = UserId.FromGuid(userContext.UserId);

        if (userIdOutcome.IsFailure)
            return userIdOutcome.Fault;

        UserId userId = userIdOutcome.Value;

        UserReadModel? user = await currentUserReadStore.GetAsync(userId.Value, cancellationToken);

        if (user is null)
            return UserOperationFaults.NotFound;

        return user;
    }
}