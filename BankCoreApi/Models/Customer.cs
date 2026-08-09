namespace BankCoreApi.Models;

/// <summary>
/// Model Entitas Customer (Nasabah/Pengguna Bank).
/// Menyimpan informasi identitas, autentikasi (password & PIN terenkripsi), dan role hak akses.
/// </summary>
public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Nomor Induk Kependudukan (16 Digit). Harus unik dan tervalidasi.
    /// </summary>
    public string Nik { get; set; } = string.Empty;

    /// <summary>
    /// Nama Lengkap Nasabah.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Alamat Email untuk login dan notifikasi.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hash Password (menggunakan PBKDF2 SHA256 dengan Salt). Tidak boleh menyimpan plaintext!
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Hash PIN Transaksi (6-digit angka). Diperlukan saat otorisasi transaksi finansial.
    /// </summary>
    public string PinHash { get; set; } = string.Empty;

    /// <summary>
    /// Role Akses Pengguna: Customer, Teller, Auditor, Admin.
    /// Digunakan untuk Authorization (RBAC).
    /// </summary>
    public string Role { get; set; } = "Customer";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: Satu nasabah bisa memiliki banyak rekening
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}
