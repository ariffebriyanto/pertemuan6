using BankCoreApi.Services;
using FluentAssertions;
using Xunit;

namespace BankCoreApi.Tests.UnitTests;

/// <summary>
/// Unit Tests untuk PasswordHasher (Pertemuan 7 - Keamanan & Pengujian Otomatis)
/// Menguji kebenaran algoritma Hashing PBKDF2 SHA256 + Salt dan verifikasi kesesuaian Password/PIN.
/// </summary>
public class PasswordHasherTests
{
    // SUT (System Under Test) adalah objek kelas yang sedang kita uji
    private readonly PasswordHasher _sut = new();

    /// <summary>
    /// TEST 1: Memastikan method Hash() menghasilkan string terenkripsi berformat PBKDF2.
    /// </summary>
    [Fact]
    public void Hash_ShouldReturnFormattedHash_WhenInputIsValid()
    {
        // === 1. ARRANGE (Persiapan Data Input) ===
        // Password/PIN mentah yang dimasukkan oleh nasabah
        string rawPassword = "SecureBankPass123!";

        // === 2. ACT (Eksekusi Method yang Diuji) ===
        // Panggil method Hash() untuk mengenkripsi password mentah
        string hashedPassword = _sut.Hash(rawPassword);

        // === 3. ASSERT (Verifikasi Hasil) ===
        // Memastikan hasil enkripsi tidak kosong
        hashedPassword.Should().NotBeNullOrEmpty();
        
        // Memastikan format hash mengandung titik pemisah (format: {iterations}.{salt}.{hash})
        hashedPassword.Should().Contain(".");
    }

    /// <summary>
    /// TEST 2: Memastikan method Verify() bernilai TRUE jika password/PIN yang dimasukkan cocok.
    /// </summary>
    [Fact]
    public void Verify_ShouldReturnTrue_WhenPasswordMatches()
    {
        // === 1. ARRANGE ===
        string rawPassword = "SecureBankPass123!";
        string hashedPassword = _sut.Hash(rawPassword); // Hasil hash asli

        // === 2. ACT ===
        // Verifikasi apakah password mentah cocok dengan hash asli di database
        bool result = _sut.Verify(rawPassword, hashedPassword);

        // === 3. ASSERT ===
        // Hasil harus bernilai true (Cocok / Login Berhasil)
        result.Should().BeTrue("karena password mentah cocok dengan hash asli");
    }

    /// <summary>
    /// TEST 3: Memastikan method Verify() bernilai FALSE jika password/PIN yang dimasukkan SALAH.
    /// </summary>
    [Fact]
    public void Verify_ShouldReturnFalse_WhenPasswordIsIncorrect()
    {
        // === 1. ARRANGE ===
        string rawPassword = "SecureBankPass123!";
        string wrongPassword = "WrongPassword123!"; // Password salah
        string hashedPassword = _sut.Hash(rawPassword);

        // === 2. ACT ===
        // Verifikasi password salah terhadap hash asli
        bool result = _sut.Verify(wrongPassword, hashedPassword);

        // === 3. ASSERT ===
        // Hasil harus bernilai false (Ditolak / Login Gagal)
        result.Should().BeFalse("karena password yang dimasukkan tidak sesuai dengan hash");
    }
}
