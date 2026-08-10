using System.Security.Cryptography;

namespace BankCoreApi.Services;

/// <summary>
/// Service Implementasi Keamanan Hashing Menggunakan PBKDF2 (SHA256) dengan Salt.
/// Sesuai dengan rekomendasi standar keamanan industri perbankan & OWASP (Open Web Application Security Project).
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    // 1. SaltSize = 16 Bytes (128 bits).
    // Salt adalah data acak unik yang digabungkan dengan kata sandi agar hash yang dihasilkan selalu berbeda,
    // meskipun dua pengguna memiliki kata sandi yang sama. Ini memproteksi dari Rainbow Table Attack.
    private const int SaltSize = 16; 

    // 2. KeySize = 32 Bytes (256 bits).
    // Panjang kunci / byte hash akhir yang akan dihasilkan oleh algoritma PBKDF2.
    private const int KeySize = 32;  

    // 3. Iterations = 100.000 (Seratus Ribu Iterasi).
    // Jumlah perulangan kalkulasi hash. Semakin tinggi nilainya, semakin berat dan lambat bagi peretas 
    // untuk melakukan serangan Brute Force atau Dictionary Attack.
    private const int Iterations = 100000; 

    // 4. HashAlgorithmName.SHA256
    // Algoritma cryptographic hash standar perbankan yang digunakan di dalam fungsi PBKDF2.
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    /// <summary>
    /// Mengubah kata sandi atau PIN mentah menjadi nilai Hash acak terenkripsi yang aman untuk disimpan ke database.
    /// </summary>
    /// <param name="input">Kata sandi atau PIN mentah dari pengguna (contoh: "BudiPass123!")</param>
    /// <returns>String terformat: {Iterasi}.{SaltBase64}.{HashBase64}</returns>
    public string Hash(string input)
    {
        // Langkah A: Hasilkan 16 byte acak kriptografik sebagai Salt unik
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Langkah B: Derivasi kata sandi + salt menggunakan algoritma PBKDF2 sebanyak 100.000 iterasi
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            input,
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);

        // Langkah C: Format string penyimpanan gabungan agar saat verifikasi login nanti,
        // sistem tahu berapa jumlah iterasi dan nilai salt yang digunakan saat hashing dilakukan.
        // Format: "100000.Base64Salt.Base64Hash"
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Memverifikasi apakah kata sandi atau PIN yang diinputkan pengguna saat login cocok dengan Hash yang tersimpan di database.
    /// </summary>
    /// <param name="input">Kata sandi mentah yang diinput pengguna saat login</param>
    /// <param name="storedHash">String Hash lengkap dari database (Format: {Iterasi}.{SaltBase64}.{HashBase64})</param>
    /// <returns>True jika cocok, False jika salah</returns>
    public bool Verify(string input, string storedHash)
    {
        // Langkah A: Memecah string hash dari database berdasarkan pemisah tanda titik '.'
        var parts = storedHash.Split('.');
        if (parts.Length != 3)
            return false; // Format tidak valid

        // Langkah B: Mengambil kembali nilai Iterasi, Salt, dan Hash asli dari database
        int iterations = int.Parse(parts[0]);
        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] hash = Convert.FromBase64String(parts[2]);

        // Langkah C: Meng-hash ulang kata sandi yang BARU dimasukkan pengguna saat login
        // menggunakan Salt dan Iterasi yang SAMA seperti saat registrasi dahulu.
        byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(
            input,
            salt,
            iterations,
            HashAlgorithm,
            hash.Length);

        // Langkah D: Membandingkan hash asli vs hash baru menggunakan FixedTimeEquals.
        // PENTING: CryptographicOperations.FixedTimeEquals digunakan untuk mencegah TIMING ATTACK.
        // Fungsi ini membandingkan byte demi byte dengan waktu komputasi konstan (tidak langsung return false di byte pertama yang beda),
        // sehingga peretas tidak bisa mengukur jeda mikrodetik CPU untuk menebak isi hash.
        return CryptographicOperations.FixedTimeEquals(hash, inputHash);
    }
}

