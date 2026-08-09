namespace BankCoreApi.Dtos;

/// <summary>
/// DTO Request Transfer Dana Antar Rekening
/// </summary>
public record TransferRequest(
    string SourceAccountNumber,
    string TargetAccountNumber,
    decimal Amount,
    string Pin,
    string Note = "Transfer"
);

/// <summary>
/// DTO Request Setor Tunai (Deposit)
/// </summary>
public record DepositRequest(
    string AccountNumber,
    decimal Amount,
    string Note = "Setor Tunai"
);

/// <summary>
/// DTO Request Tarik Tunai (Withdrawal)
/// </summary>
public record WithdrawalRequest(
    string AccountNumber,
    decimal Amount,
    string Pin,
    string Note = "Tarik Tunai"
);

/// <summary>
/// DTO Response Riwayat Transaksi / Buku Kas
/// </summary>
public record TransactionResponse(
    Guid Id,
    string TransactionReference,
    string? SourceAccountNumber,
    string? TargetAccountNumber,
    string TransactionType,
    decimal Amount,
    string Note,
    DateTime CreatedAt
);
