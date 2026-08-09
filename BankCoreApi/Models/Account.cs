namespace BankCoreApi.Models;

/// <summary>
/// Model Entitas Rekening Bank (Account).
/// Menyimpan saldo nasabah, jenis rekening, serta nomor rekening unik.
/// </summary>
public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Nomor Rekening unik (10 digit angka).
    /// </summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Foreign Key mengacu pada Nasabah pemilik rekening.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Navigation property ke Nasabah.
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Saldo Rekening saat ini. Menggunakan tipe decimal untuk presisi uang.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Jenis Rekening (contoh: "Savings" / Tabungan, "Checking" / Giro).
    /// </summary>
    public string AccountType { get; set; } = "Savings";

    /// <summary>
    /// Status Rekening Aktif atau Diblokir.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
