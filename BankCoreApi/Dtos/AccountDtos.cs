namespace BankCoreApi.Dtos;

/// <summary>
/// DTO Request Pembukaan Rekening Baru
/// </summary>
public record CreateAccountRequest(
    Guid CustomerId,
    string AccountType = "Savings",
    decimal InitialDeposit = 50000m
);

/// <summary>
/// DTO Response Ringkasan Informasi Rekening Nasabah
/// </summary>
public record AccountResponse(
    Guid Id,
    string AccountNumber,
    Guid CustomerId,
    string CustomerName,
    decimal Balance,
    string AccountType,
    bool IsActive,
    DateTime CreatedAt
);
