using System.Security.Cryptography;

namespace BankCoreApi.Services;

/// <summary>
/// Implementasi Keamanan Hashing Menggunakan PBKDF2 (SHA256) dengan Salt.
/// Sesuai dengan rekomendasi standar keamanan industri perbankan (OWASP).
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32;  // 256 bit
    private const int Iterations = 100000; // 100k iterasi untuk ketahanan Brute Force
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    public string Hash(string input)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            input,
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);

        // Format simpan: {Iterations}.{SaltBase64}.{HashBase64}
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string input, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3)
            return false;

        int iterations = int.Parse(parts[0]);
        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] hash = Convert.FromBase64String(parts[2]);

        byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(
            input,
            salt,
            iterations,
            HashAlgorithm,
            hash.Length);

        return CryptographicOperations.FixedTimeEquals(hash, inputHash);
    }
}
