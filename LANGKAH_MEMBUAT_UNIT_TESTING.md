# Panduan Langkah demi Langkah Membuat Unit Testing di .NET (C#)

Dokumen ini berisi panduan praktis dan terstruktur tentang cara membuat **Unit Testing** pada aplikasi .NET (C#) menggunakan framework **xUnit**, **Moq**, **FluentAssertions**, dan **Entity Framework Core In-Memory Database**.

---

## Daftar Isi
1. [Prasyarat & Alat yang Digunakan](#1-prasyarat--alat-yang-digunakan)
2. [Langkah 1: Membuat Project Unit Test](#langkah-1-membuat-project-unit-test)
3. [Langkah 2: Menambahkan Referensi Project & Package NuGet](#langkah-2-menambahkan-referensi-project--package-nuget)
4. [Langkah 3: Menyusun Struktur Folder & Konvensi Penamaan](#langkah-3-menyusun-struktur-folder--konvensi-penamaan)
5. [Langkah 4: Memahami Pola AAA (Arrange, Act, Assert)](#langkah-4-memahami-pola-aaa-arrange-act-assert)
6. [Langkah 5: Menulis Kode Unit Test (Studi Kasus Transaksi Bank)](#langkah-5-menulis-kode-unit-test-studi-kasus-transaksi-bank)
7. [Langkah 6: Menggunakan Moq untuk Memalsukan Dependensi](#langkah-6-menggunakan-moq-untuk-memalsukan-dependensi)
8. [Langkah 7: Menjalankan Pengujian (Testing)](#langkah-7-menjalankan-pengujian-testing)
9. [Tips & Best Practices Unit Testing](#tips--best-practices-unit-testing)

---

## 1. Prasyarat & Alat yang Digunakan

Dalam ekosistem .NET, pustaka standar yang digunakan untuk pengujian adalah:
- **xUnit**: Framework pengujian unit modern untuk .NET.
- **Moq**: Pustaka *mocking* untuk memalsukan (*mock*) dependensi/interface.
- **FluentAssertions**: Pustaka klausa *assertion* agar kodingan tes mudah dibaca manusia (*human-readable*).
- **Microsoft.EntityFrameworkCore.InMemory**: Database sementara di RAM untuk pengujian terisolasi tanpa butuh database SQL nyata.

---

## Langkah 1: Membuat Project Unit Test

Buka terminal di direktori solusi project Anda (sejajar dengan `.sln` atau project utama), lalu jalankan perintah:

```bash
# 1. Buat project xUnit baru bernama BankCoreApi.Tests
dotnet new xunit -n BankCoreApi.Tests

# 2. Masukkan project test ke dalam File Solution (.sln)
dotnet sln add BankCoreApi.Tests/BankCoreApi.Tests.csproj
```

---

## Langkah 2: Menambahkan Referensi Project & Package NuGet

Project test harus bisa mengakses kelas dan method yang ada di project utama (`BankCoreApi`), serta membutuhkan pustaka penguji.

```bash
# 1. Tambahkan referensi dari BankCoreApi.Tests ke BankCoreApi
dotnet add BankCoreApi.Tests reference BankCoreApi/BankCoreApi.csproj

# 2. Masuk ke folder project test
cd BankCoreApi.Tests

# 3. Install Package NuGet yang dibutuhkan
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

Isi file `BankCoreApi.Tests.csproj` akan terlihat seperti ini:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="7.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\BankCoreApi\BankCoreApi.csproj" />
  </ItemGroup>
</Project>
```

---

## Langkah 3: Menyusun Struktur Folder & Konvensi Penamaan

Buat struktur folder yang rapi di dalam `BankCoreApi.Tests`:

```text
BankCoreApi.Tests/
├── UnitTests/
│   ├── PasswordHasherTests.cs
│   └── TransactionServiceTests.cs
└── IntegrationTests/
    ├── CustomWebApplicationFactory.cs
    ├── AuthEndpointsTests.cs
    └── TransactionEndpointsTests.cs
```

### Konvensi Penamaan

1. **Nama File/Kelas Test**: `<NamaKelasYangDiuji>Tests.cs`
   - Contoh: `TransactionServiceTests.cs` menguji `TransactionService.cs`.
2. **Nama Method Test**: `<NamaMethod>_<EkspektasiRespon>_<SkenarioKondisi>`
   - Contoh: `TransferAsync_ShouldMutateBalances_WhenRequestIsValid`
   - Contoh: `TransferAsync_ShouldThrowUnauthorizedAccessException_WhenPinIsInvalid`

---

## Langkah 4: Memahami Pola AAA (Arrange, Act, Assert)

Setiap method pengujian unit harus mengikuti pola **AAA**:

```csharp
[Fact]
public async Task NamaMethod_Ekspektasi_Kondisi()
{
    // 1. ARRANGE (Persiapan Data & Object)
    // Buat data dummy, persiapkan mock, instansiasi kelas yang diuji (SUT).

    // 2. ACT (Eksekusi Method)
    // Panggil fungsi/method yang ingin diuji.

    // 3. ASSERT (Verifikasi Hasil)
    // Pastikan output atau perubahan data sesuai dengan ekspektasi.
}
```

---

## Langkah 5: Menulis Kode Unit Test (Studi Kasus Transaksi Bank)

Buat file baru `UnitTests/TransactionServiceTests.cs`. Berikut adalah contoh kode lengkap unit test untuk fitur Transfer:

```csharp
using BankCoreApi.Data;
using BankCoreApi.Dtos;
using BankCoreApi.Models;
using BankCoreApi.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BankCoreApi.Tests.UnitTests;

public class TransactionServiceTests
{
    private readonly BankDbContext _dbContext;
    private readonly Mock<IPasswordHasher> _mockHasher;
    private readonly TransactionService _sut; // SUT = System Under Test

    public TransactionServiceTests()
    {
        // 1. Inisialisasi Database In-Memory unik untuk setiap tes (Terisolasi)
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new BankDbContext(options);

        // 2. Buat Mock untuk IPasswordHasher
        _mockHasher = new Mock<IPasswordHasher>();

        // 3. Instansiasi TransactionService dengan DbContext In-Memory & Mock Hasher
        _sut = new TransactionService(_dbContext, _mockHasher.Object);
    }

    /// <summary>
    /// SKENARIO 1: Transfer Berhasil -> Saldo terpotong di rekening asal & bertambah di rekening tujuan.
    /// </summary>
    [Fact]
    public async Task TransferAsync_ShouldMutateBalances_WhenRequestIsValid()
    {
        // === 1. ARRANGE ===
        var customer = new Customer
        {
            Nik = "3171000000000099",
            FullName = "Ahmad Dani",
            Email = "ahmad@gmail.com",
            PinHash = "hashed_123456"
        };
        _dbContext.Customers.Add(customer);

        // Rekening Asal: Saldo Rp 1.000.000
        var sourceAccount = new Account
        {
            AccountNumber = "1001111111",
            CustomerId = customer.Id,
            Customer = customer,
            Balance = 1000000m,
            IsActive = true
        };

        // Rekening Tujuan: Saldo Rp 200.000
        var targetAccount = new Account
        {
            AccountNumber = "1002222222",
            CustomerId = customer.Id,
            Balance = 200000m,
            IsActive = true
        };

        _dbContext.Accounts.AddRange(sourceAccount, targetAccount);
        await _dbContext.SaveChangesAsync();

        // Setup Mock: Jika PIN "123456" dicocokkan dengan "hashed_123456", kembalikan TRUE
        _mockHasher.Setup(h => h.Verify("123456", "hashed_123456")).Returns(true);

        var request = new TransferRequest(
            SourceAccountNumber: "1001111111",
            TargetAccountNumber: "1002222222",
            Amount: 300000m,
            Pin: "123456",
            Note: "Bayar Utang"
        );

        // === 2. ACT ===
        var response = await _sut.TransferAsync(request);

        // === 3. ASSERT ===
        response.Should().NotBeNull();
        response.TransactionType.Should().Be("Transfer");
        response.Amount.Should().Be(300000m);

        var updatedSource = await _dbContext.Accounts.FindAsync(sourceAccount.Id);
        var updatedTarget = await _dbContext.Accounts.FindAsync(targetAccount.Id);

        // Saldo Asal: Rp 1.000.000 - Rp 300.000 = Rp 700.000
        updatedSource!.Balance.Should().Be(700000m);

        // Saldo Tujuan: Rp 200.000 + Rp 300.000 = Rp 500.000
        updatedTarget!.Balance.Should().Be(500000m);
    }

    /// <summary>
    /// SKENARIO 2: Transfer Gagal karena PIN Salah -> Menghasilkan Exception.
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

        // Setup Mock: PIN "999999" (Salah) -> Kembalikan FALSE
        _mockHasher.Setup(h => h.Verify("999999", "hashed_123456")).Returns(false);

        var request = new TransferRequest("1003333333", "1004444444", 50000m, "999999");

        // === 2. ACT & 3. ASSERT ===
        Func<Task> act = async () => await _sut.TransferAsync(request);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*PIN Transaksi salah*");
    }
}
```

---

## Langkah 6: Menggunakan Moq untuk Memalsukan Dependensi

Jika kelas yang diuji membutuhkan service eksternal (misal API pihak ke-3, Email Service, atau Hashing Service), kita tidak perlu memanggil service aslinya. Gunakan **Moq**:

```csharp
// 1. Buat Objek Mock
var mockHasher = new Mock<IPasswordHasher>();

// 2. Tentukan Perilaku (Setup)
// Jika method Verify dipanggil dengan input apapun, kembalikan true
mockHasher.Setup(m => m.Verify(It.IsAny<string>(), It.IsAny<string>()))
          .Returns(true);

// 3. Masukkan Objek Mock ke Constructor Service
var service = new TransactionService(dbContext, mockHasher.Object);

// 4. Verifikasi bahwa method pada Mock benar-benar dipanggil (Opsional)
mockHasher.Verify(m => m.Verify("123456", "hashed_123456"), Times.Once);
```

---

## Langkah 7: Menjalankan Pengujian (Testing)

Anda bisa menjalankan test melalui **Terminal** atau **IDE Visual Studio / VS Code**.

### 1. Menggunakan CLI Terminal
Jalankan perintah ini di root folder solusi:

```bash
dotnet test
```

Untuk melihat detail hasil test:
```bash
dotnet test --logger "console;verbosity=detailed"
```

Output jika semua test berhasil:
```text
Passed!  - Failed:     0, Passed:    12, Skipped:     0, Total:    12, Duration: 1 s - BankCoreApi.Tests.dll (net9.0)
```

### 2. Menggunakan Visual Studio / VS Code Test Explorer
- **VS Code**: Install ekstensi *C# Dev Kit* atau *Test Explorer UI*. Klik ikon tab Test pada sidebar kiri.
- **Visual Studio**: Buka menu `Test` -> `Run All Tests` (Ctrl + R, A).

---

## Tips & Best Practices Unit Testing

1. **Fokus pada 1 Unit / Fungsi**: Tes hanya satu method logika bisnis dalam satu fungsi pengujian.
2. **Independen & Terisolasi**: Setiap test tidak boleh saling menggantungkan data dari test lainnya. Gunakan `Guid.NewGuid().ToString()` untuk nama DB In-Memory agar DB baru dibuat setiap kali test dijalankan.
3. **Cepat**: Unit test harus berjalan sangat cepat (beberapa milidetik per tes).
4. **Hindari Logic di Dalam Test**: Jangan gunakan `if`, `for`, atau `switch` di dalam kode unit test.
5. **Gunakan Atribut `[Fact]` dan `[Theory]`**:
   - `[Fact]`: Digunakan untuk pengujian skenario tunggal bernilai konstan.
   - `[Theory]` + `[InlineData]`: Digunakan jika ingin menguji method yang sama dengan berbagai macam input data variatif.

---
