using BankCoreApi.Dtos;

namespace BankCoreApi.Services;

/// <summary>
/// Interface Service Transaksi Perbankan (Transfer, Setor, Tarik, Rekening Koran)
/// </summary>
public interface ITransactionService
{
    Task<TransactionResponse> TransferAsync(TransferRequest request);
    Task<TransactionResponse> DepositAsync(DepositRequest request);
    Task<TransactionResponse> WithdrawalAsync(WithdrawalRequest request);
    Task<IEnumerable<TransactionResponse>> GetStatementAsync(string accountNumber);
}
