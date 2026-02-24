using System.Security.Cryptography;
using System.Text;

using SharedKernel.Application.Authentication;

namespace SharedKernel.Infrastructure.Authentication;

public sealed class SecretHasher : ISecretHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 500000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    public string Hash(string secret)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(secret, salt, Iterations, Algorithm, HashSize);

        return $"{Convert.ToHexString(hash)}-{Convert.ToHexString(salt)}";
    }

    public string HashDeterministic(string secret)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    }

    public bool Verify(string secret, string hashedSecret)
    {
        ArgumentNullException.ThrowIfNull(hashedSecret);

        string[] parts = hashedSecret.Split('-');
        byte[] hash = Convert.FromHexString(parts[0]);
        byte[] salt = Convert.FromHexString(parts[1]);

        byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(secret, salt, Iterations, Algorithm, HashSize);

        return CryptographicOperations.FixedTimeEquals(hash, inputHash);
    }
}