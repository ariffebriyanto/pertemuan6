using BankCoreApi.Data;
using BankCoreApi.Dtos;
using BankCoreApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BankCoreApi.Services;

/// <summary>
/// Service Implementasi Pemrosesan Transaksi Keuangan dan Pencatatan Ledger (Pertemuan 7 & 9 Security & Hardening)
/// </summary>
public class TransactionService : ITransactionService
{
    private readonly BankDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public TransactionService(BankDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<TransactionResponse> TransferAsync(TransferRequest request)
    {
        // 1. Validasi nominal transfer harus positif
        if (request.Amount <= 0)
            throw new ArgumentException("Nominal transfer harus lebih besar dari 0.");

        // 2. Mencegah transfer ke rekening yang sama
        if (request.SourceAccountNumber == request.TargetAccountNumber)
            throw new InvalidOperationException("Rekening asal dan rekening tujuan tidak boleh sama.");

        // 3. Ambil Rekening Asal & Tujuan
        var sourceAccount = await _dbContext.Accounts
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.AccountNumber == request.SourceAccountNumber);

        var targetAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == request.TargetAccountNumber);

        if (sourceAccount == null || !sourceAccount.IsActive)
            throw new KeyNotFoundException("Rekening asal tidak ditemukan atau tidak aktif.");

        if (targetAccount == null || !targetAccount.IsActive)
            throw new KeyNotFoundException("Rekening tujuan tidak ditemukan atau tidak aktif.");

        // 4. Verifikasi PIN Transaksi Nasabah
        if (sourceAccount.Customer == null || !_passwordHasher.Verify(request.Pin, sourceAccount.Customer.PinHash))
            throw new UnauthorizedAccessException("PIN Transaksi salah.");

        // 5. Cek Kecukupan Saldo (Overdraft Protection)
        if (sourceAccount.Balance < request.Amount)
            throw new InvalidOperationException("Saldo tidak mencukupi untuk melakukan transfer.");

        // 6. Eksekusi Mutasi Saldo Atomik
        sourceAccount.Balance -= request.Amount;
        targetAccount.Balance += request.Amount;

        // 7. Catat ke Ledger / Jurnal Transaksi
        var transaction = new Transaction
        {
            TransactionReference = GenerateReference("TRX"),
            SourceAccountId = sourceAccount.Id,
            TargetAccountId = targetAccount.Id,
            TransactionType = "Transfer",
            Amount = request.Amount,
            Note = request.Note
        };

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        return MapToResponse(transaction, sourceAccount.AccountNumber, targetAccount.AccountNumber);
    }

    public async Task<TransactionResponse> DepositAsync(DepositRequest request)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Nominal setor tunai harus lebih besar dari 0.");

        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == request.AccountNumber);

        if (account == null || !account.IsActive)
            throw new KeyNotFoundException("Rekening tidak ditemukan atau tidak aktif.");

        account.Balance += request.Amount;

        var transaction = new Transaction
        {
            TransactionReference = GenerateReference("DEP"),
            SourceAccountId = null,
            TargetAccountId = account.Id,
            TransactionType = "Deposit",
            Amount = request.Amount,
            Note = request.Note
        };

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        return MapToResponse(transaction, null, account.AccountNumber);
    }

    public async Task<TransactionResponse> WithdrawalAsync(WithdrawalRequest request)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Nominal tarik tunai harus lebih besar dari 0.");

        var account = await _dbContext.Accounts
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.AccountNumber == request.AccountNumber);

        if (account == null || !account.IsActive)
            throw new KeyNotFoundException("Rekening tidak ditemukan atau tidak aktif.");

        if (account.Customer == null || !_passwordHasher.Verify(request.Pin, account.Customer.PinHash))
            throw new UnauthorizedAccessException("PIN Transaksi salah.");

        if (account.Balance < request.Amount)
            throw new InvalidOperationException("Saldo tidak mencukupi untuk tarik tunai.");

        account.Balance -= request.Amount;

        var transaction = new Transaction
        {
            TransactionReference = GenerateReference("WTH"),
            SourceAccountId = account.Id,
            TargetAccountId = null,
            TransactionType = "Withdrawal",
            Amount = request.Amount,
            Note = request.Note
        };

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        return MapToResponse(transaction, account.AccountNumber, null);
    }

    public async Task<IEnumerable<TransactionResponse>> GetStatementAsync(string accountNumber)
    {
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

        if (account == null)
            throw new KeyNotFoundException($"Rekening {accountNumber} tidak ditemukan.");

        var transactions = await _dbContext.Transactions
            .Where(t => t.SourceAccountId == account.Id || t.TargetAccountId == account.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var accountLookup = await _dbContext.Accounts
            .ToDictionaryAsync(a => a.Id, a => a.AccountNumber);

        return transactions.Select(t => MapToResponse(
            t,
            t.SourceAccountId.HasValue && accountLookup.TryGetValue(t.SourceAccountId.Value, out var src) ? src : null,
            t.TargetAccountId.HasValue && accountLookup.TryGetValue(t.TargetAccountId.Value, out var tgt) ? tgt : null
        ));
    }

    private static string GenerateReference(string prefix)
    {
        return $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    private static TransactionResponse MapToResponse(Transaction t, string? srcAcc, string? tgtAcc)
    {
        return new TransactionResponse(
            Id: t.Id,
            TransactionReference: t.TransactionReference,
            SourceAccountNumber: srcAcc,
            TargetAccountNumber: tgtAcc,
            TransactionType: t.TransactionType,
            Amount: t.Amount,
            Note: t.Note,
            CreatedAt: t.CreatedAt
        );
    }
}
