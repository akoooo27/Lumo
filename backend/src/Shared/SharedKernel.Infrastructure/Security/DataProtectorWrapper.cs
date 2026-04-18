using Microsoft.AspNetCore.DataProtection;

using SharedKernel.Application.Security;

namespace SharedKernel.Infrastructure.Security;

internal sealed class DataProtectorWrapper : IDataProtectorWrapper
{
    private readonly IDataProtector _protector;

    public DataProtectorWrapper(IDataProtectionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _protector = provider.CreateProtector("Lumo.DataProtection");
    }

    public string Protect(string token) => _protector.Protect(token);

    public string Unprotect(string protectedToken) => _protector.Unprotect(protectedToken);
}