using FastEndpoints;

using SharedKernel;
using SharedKernel.Api.Infrastructure;

namespace Notifications.Api.Endpoints;

internal abstract class BaseEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    protected async Task SendOutcomeAsync<T>
    (
        Outcome<T> outcome,
        Func<T, TResponse> mapper,
        int successStatusCode = 200,
        CancellationToken cancellationToken = default
    )
    {
        if (outcome.IsFailure)
        {
            await Send.ResultAsync(CustomResults.Problem(outcome, HttpContext));
            return;
        }

        TResponse response = mapper(outcome.Value);

        await Send.ResponseAsync(response, successStatusCode, cancellationToken);
    }
}

internal abstract class BaseEndpoint<TRequest> : Endpoint<TRequest>
    where TRequest : notnull
{
    protected async Task SendOutcomeAsync
    (
        Outcome outcome,
        CancellationToken cancellationToken = default
    )
    {
        if (outcome.IsFailure)
        {
            await Send.ResultAsync(CustomResults.Problem(outcome, HttpContext));
            return;
        }

        await Send.NoContentAsync(cancellationToken);
    }
}