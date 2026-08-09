namespace BankCoreApi.Models;

/// <summary>
/// Model Entitas Transaksi Keuangan (Financial Ledger).
/// Mencatat setiap mutasi (Deposit, Withdrawal, Transfer) secara immutable untuk jejak audit perbankan.
/// </summary>
public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Nomor Referensi Transaksi Unik (contoh: TRX-20260809-ABC12345).
    /// </summary>
    public string TransactionReference { get; set; } = string.Empty;

    /// <summary>
    /// ID Rekening Asal (Kosong jika Setor Tunai/Deposit).
    /// </summary>
    public Guid? SourceAccountId { get; set; }

    /// <summary>
    /// ID Rekening Tujuan (Kosong jika Tarik Tunai/Withdrawal).
    /// </summary>
    public Guid? TargetAccountId { get; set; }

    /// <summary>
    /// Jenis Transaksi: "Deposit", "Withdrawal", "Transfer".
    /// </summary>
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>
    /// Nominal Transaksi. Selalu bernilai positif.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Catatan / Berita Transaksi.
    /// </summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>
    /// Waktu terjadinya transaksi (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
