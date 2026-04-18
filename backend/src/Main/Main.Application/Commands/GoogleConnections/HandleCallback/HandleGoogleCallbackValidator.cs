using FluentValidation;

namespace Main.Application.Commands.GoogleConnections.HandleCallback;

internal sealed class HandleGoogleCallbackValidator : AbstractValidator<HandleGoogleCallbackCommand>
{
    public HandleGoogleCallbackValidator()
    {
        RuleFor(hgcc => hgcc.Code)
            .NotEmpty().WithMessage("Authorization code is required.");

        RuleFor(hgcc => hgcc.State)
            .NotEmpty().WithMessage("OAuth state parameter is required.");
    }
}