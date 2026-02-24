using SharedKernel;

namespace Main.Application.Abstractions.Services;

public interface IEphemeralChatAccessValidator
{
    Task<Outcome> ValidateAccessAsync(string ephemeralChatId, CancellationToken cancellationToken);
}