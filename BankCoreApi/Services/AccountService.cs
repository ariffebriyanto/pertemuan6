using BankCoreApi.Data;
using BankCoreApi.Dtos;
using BankCoreApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BankCoreApi.Services;

/// <summary>
/// Service Implementasi Pengelolaan Rekening (Buka Rekening, Cek Saldo)
/// </summary>
public class AccountService : IAccountService
{
    private readonly BankDbContext _dbContext;

    public AccountService(BankDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request)
    {
        // 1. Verifikasi Keberadaan Nasabah
        var customer = await _dbContext.Customers.FindAsync(request.CustomerId);
        if (customer == null)
            throw new KeyNotFoundException("Nasabah tidak ditemukan.");

        // 2. Setoran awal minimal Rp 50.000
        if (request.InitialDeposit < 50000m)
            throw new InvalidOperationException("Setoran awal minimal adalah Rp 50.000.");

        // 3. Generate Nomor Rekening Unik 10 Digit
        string accountNumber = await GenerateUniqueAccountNumberAsync();

        var account = new Account
        {
            AccountNumber = accountNumber,
            CustomerId = customer.Id,
            Customer = customer,
            Balance = request.InitialDeposit,
            AccountType = request.AccountType,
            IsActive = true
        };

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();

        return MapToResponse(account, customer.FullName);
    }

    public async Task<AccountResponse> GetAccountByNumberAsync(string accountNumber)
    {
        var account = await _dbContext.Accounts
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

        if (account == null)
            throw new KeyNotFoundException($"Rekening {accountNumber} tidak ditemukan.");

        return MapToResponse(account, account.Customer?.FullName ?? string.Empty);
    }

    public async Task<IEnumerable<AccountResponse>> GetCustomerAccountsAsync(Guid customerId)
    {
        var accounts = await _dbContext.Accounts
            .Include(a => a.Customer)
            .Where(a => a.CustomerId == customerId)
            .ToListAsync();

        return accounts.Select(a => MapToResponse(a, a.Customer?.FullName ?? string.Empty));
    }

    private async Task<string> GenerateUniqueAccountNumberAsync()
    {
        var random = new Random();
        string accountNumber;
        do
        {
            // Format: 100 + 7 digit acak (misal: 1007890123)
            accountNumber = $"100{random.Next(1000007, 9999999)}";
        } while (await _dbContext.Accounts.AnyAsync(a => a.AccountNumber == accountNumber));

        return accountNumber;
    }

    private static AccountResponse MapToResponse(Account account, string customerName)
    {
        return new AccountResponse(
            Id: account.Id,
            AccountNumber: account.AccountNumber,
            CustomerId: account.CustomerId,
            CustomerName: customerName,
            Balance: account.Balance,
            AccountType: account.AccountType,
            IsActive: account.IsActive,
            CreatedAt: account.CreatedAt
        );
    }
}
