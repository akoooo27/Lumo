namespace Auth.Application.Abstractions.ZeroBounce;

public interface IEmailValidationService
{
    Task<bool> IsSpamEmailAsync(string emailAddress, CancellationToken cancellationToken);
}