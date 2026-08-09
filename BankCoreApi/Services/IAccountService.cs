using BankCoreApi.Dtos;

namespace BankCoreApi.Services;

/// <summary>
/// Interface Service Manajemen Rekening Perbankan
/// </summary>
public interface IAccountService
{
    Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request);
    Task<AccountResponse> GetAccountByNumberAsync(string accountNumber);
    Task<IEnumerable<AccountResponse>> GetCustomerAccountsAsync(Guid customerId);
}
