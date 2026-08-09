using BankCoreApi.Data;
using BankCoreApi.Dtos;
using BankCoreApi.Models;
using BankCoreApi.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BankCoreApi.Tests.UnitTests;

/// <summary>
/// Unit Tests untuk Pemrosesan Transaksi Perbankan (Pertemuan 7 - Logika Bisnis & Pengujian Terisolasi)
/// Menguji aturan bisnis perbankan: mutasi saldo atomik, verifikasi PIN, overdraft protection, dan pencegahan self-transfer.
/// </summary>
public class TransactionServiceTests
{
    private readonly BankDbContext _dbContext;
    private readonly Mock<IPasswordHasher> _mockHasher;
    private readonly TransactionService _sut; // SUT = System Under Test (TransactionService)

    public TransactionServiceTests()
    {
        // 1. Inisialisasi Database In-Memory khusus untuk setiap unit test (Terisolasi)
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new BankDbContext(options);

        // 2. Buat Mock untuk PasswordHasher menggunakan Moq (Memalsukan fungsi verifikasi PIN agar tes cepat)
        _mockHasher = new Mock<IPasswordHasher>();

        // 3. Instansiasi TransactionService dengan DbContext in-memory dan Mock Hasher
        _sut = new TransactionService(_dbContext, _mockHasher.Object);
    }

    /// <summary>
    /// TEST 1: Skenario Transfer Berhasil
    /// Menguji bahwa saldo rekening asal terpotong dan saldo rekening tujuan bertambah secara atomik saat data valid & PIN benar.
    /// </summary>
    [Fact]
    public async Task TransferAsync_ShouldMutateBalances_WhenRequestIsValid()
    {
        // === 1. ARRANGE (Persiapan Data Dummy) ===
        // Buat nasabah dummy pemilik rekening asal
        var customer = new Customer
        {
            Nik = "3171000000000099",
            FullName = "Ahmad Dani",
            Email = "ahmad@gmail.com",
            PinHash = "hashed_123456"
        };
        _dbContext.Customers.Add(customer);

        // Rekening Asal: Saldo Awal Rp 1.000.000
        var sourceAccount = new Account
        {
            AccountNumber = "1001111111",
            CustomerId = customer.Id,
            Customer = customer,
            Balance = 1000000m,
            IsActive = true
        };

        // Rekening Tujuan: Saldo Awal Rp 200.000
        var targetAccount = new Account
        {
            AccountNumber = "1002222222",
            CustomerId = customer.Id,
            Balance = 200000m,
            IsActive = true
        };

        _dbContext.Accounts.AddRange(sourceAccount, targetAccount);
        await _dbContext.SaveChangesAsync();

        // Konfigurasi Mock: Ketika PIN "123456" diverifikasi terhadap "hashed_123456", kembalikan TRUE
        _mockHasher.Setup(h => h.Verify("123456", "hashed_123456")).Returns(true);

        // Request Transfer Rp 300.000 dari 1001111111 ke 1002222222
        var request = new TransferRequest(
            SourceAccountNumber: "1001111111",
            TargetAccountNumber: "1002222222",
            Amount: 300000m,
            Pin: "123456",
            Note: "Bayar Utang"
        );

        // === 2. ACT (Eksekusi Transfer) ===
        var response = await _sut.TransferAsync(request);

        // === 3. ASSERT (Verifikasi Saldo Akhir) ===
        // Memastikan respon transaksi bernilai valid
        response.Should().NotBeNull();
        response.TransactionType.Should().Be("Transfer");
        response.Amount.Should().Be(300000m);

        // Ambil data rekening terbaru dari database
        var updatedSource = await _dbContext.Accounts.FindAsync(sourceAccount.Id);
        var updatedTarget = await _dbContext.Accounts.FindAsync(targetAccount.Id);

        // Verifikasi Mutasi Saldo:
        // Saldo Asal: Rp 1.000.000 - Rp 300.000 = Rp 700.000
        updatedSource!.Balance.Should().Be(700000m, "karena saldo asal terpotong sebesar nominal transfer");

        // Saldo Tujuan: Rp 200.000 + Rp 300.000 = Rp 500.000
        updatedTarget!.Balance.Should().Be(500000m, "karena saldo tujuan bertambah sebesar nominal transfer");
    }

    /// <summary>
    /// TEST 2: Skenario PIN Transaksi Salah
    /// Menguji bahwa sistem melempar UnauthorizedAccessException saat nasabah memasukkan PIN yang salah.
    /// </summary>
    [Fact]
    public async Task TransferAsync_ShouldThrowUnauthorizedAccessException_WhenPinIsInvalid()
    {
        // === 1. ARRANGE ===
        var customer = new Customer { Nik = "3171000000000098", PinHash = "hashed_123456" };
        _dbContext.Customers.Add(customer);

        var sourceAccount = new Account { AccountNumber = "1003333333", CustomerId = customer.Id, Customer = customer, Balance = 500000m, IsActive = true };
        var targetAccount = new Account { AccountNumber = "1004444444", CustomerId = customer.Id, Balance = 100000m, IsActive = true };

        _dbContext.Accounts.AddRange(sourceAccount, targetAccount);
        await _dbContext.SaveChangesAsync();

        // Konfigurasi Mock: PIN "999999" (PIN Salah) dikembalikan FALSE oleh Hasher
        _mockHasher.Setup(h => h.Verify("999999", "hashed_123456")).Returns(false);

        var request = new TransferRequest("1003333333", "1004444444", 50000m, "999999");

        // === 2. ACT & 3. ASSERT ===
        // Memastikan panggilan method melempar UnauthorizedAccessException dengan pesan error PIN salah
        Func<Task> act = async () => await _sut.TransferAsync(request);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*PIN Transaksi salah*", "karena PIN yang dimasukkan tidak cocok");
    }

    /// <summary>
    /// TEST 3: Skenario Saldo Tidak Mencukupi (Overdraft Protection)
    /// Menguji bahwa sistem melempar InvalidOperationException jika saldo kurang dari nominal transfer.
    /// </summary>
    [Fact]
    public async Task TransferAsync_ShouldThrowInvalidOperationException_WhenInsufficientBalance()
    {
        // === 1. ARRANGE ===
        var customer = new Customer { PinHash = "hashed_123456" };
        _dbContext.Customers.Add(customer);

        // Saldo Rekening Asal hanya Rp 50.000
        var sourceAccount = new Account { AccountNumber = "1005555555", CustomerId = customer.Id, Customer = customer, Balance = 50000m, IsActive = true };
        var targetAccount = new Account { AccountNumber = "1006666666", CustomerId = customer.Id, Balance = 100000m, IsActive = true };

        _dbContext.Accounts.AddRange(sourceAccount, targetAccount);
        await _dbContext.SaveChangesAsync();

        _mockHasher.Setup(h => h.Verify("123456", "hashed_123456")).Returns(true);

        // Request Transfer Rp 500.000 (Lebih besar dari saldo 50.000)
        var request = new TransferRequest("1005555555", "1006666666", 500000m, "123456");

        // === 2. ACT & 3. ASSERT ===
        // Memastikan sistem menolak transfer akibat saldo kurang
        Func<Task> act = async () => await _sut.TransferAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Saldo tidak mencukupi*", "karena nominal transfer melebihi saldo rekening asal");
    }

    /// <summary>
    /// TEST 4: Skenario Mencegah Transfer ke Rekening Sendiri
    /// Menguji bahwa sistem menolak jika rekening asal dan rekening tujuan sama.
    /// </summary>
    [Fact]
    public async Task TransferAsync_ShouldThrowInvalidOperationException_WhenSelfTransferAttempted()
    {
        // === 1. ARRANGE ===
        // Rekening Asal & Tujuan sama (1007777777)
        var request = new TransferRequest("1007777777", "1007777777", 50000m, "123456");

        // === 2. ACT & 3. ASSERT ===
        Func<Task> act = async () => await _sut.TransferAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak boleh sama*", "karena rekening asal dan tujuan tidak boleh identik");
    }
}
