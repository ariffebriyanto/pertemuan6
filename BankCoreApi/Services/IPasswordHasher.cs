namespace BankCoreApi.Services;

/// <summary>
/// Interface Service Keamanan untuk Hashing & Verifikasi Password/PIN (Pertemuan 7 - Security)
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Meng-hash teks mentah (password/PIN) menggunakan algoritma PBKDF2 dengan Salt acak.
    /// </summary>
    string Hash(string input);

    /// <summary>
    /// Memverifikasi apakah teks mentah cocok dengan hash yang tersimpan.
    /// </summary>
    bool Verify(string input, string storedHash);
}
