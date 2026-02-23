using FluentValidation;

namespace Auth.Application.Commands.Sessions.RevokeSessions;

internal sealed class RevokeSessionsValidator : AbstractValidator<RevokeSessionsCommand>
{
    public RevokeSessionsValidator()
    {
        RuleFor(rsc => rsc.SessionIds)
            .NotEmpty().WithMessage("At least one session ID is required.");
    }
}