namespace SharedKernel.Application.Security;

public interface IDataProtectorWrapper
{
    string Protect(string token);

    string Unprotect(string protectedToken);
}