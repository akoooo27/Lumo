namespace SharedKernel.Application.Authentication;

public interface ISecretHasher
{
    string Hash(string secret);

    string HashDeterministic(string secret);

    bool Verify(string secret, string hashedSecret);
}