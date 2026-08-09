# 📖 Panduan Mentor — Pertemuan 7 & Pertemuan 9 (.NET Core Banking API)

Dokumen ini disusun khusus untuk **Mentor** guna memandu proses mengajar pada **Pertemuan 7** (Security, Auth, dan Automated Testing) dan **Pertemuan 9** (Docker, CI/CD, dan Capstone Hardening) menggunakan aplikasi contoh **BankCoreApi**.

---

## 🎯 1. Alur & Strategi Mengajar

### Modul Pertemuan 7: Keamanan, Autentikasi/Otorisasi, & Automated Testing
| Topik Utama | Komponen Kode | Poin Penjelasan Kunci untuk Mahasiswa |
| :--- | :--- | :--- |
| **Database Connection** | [appsettings.json](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi/appsettings.json) | Penggunaan **SQL Server** connection string (`Server=localhost;Database=BankCoreDb;User Id=sa;Password=PasswordBanking@123...`) dan in-memory fallback untuk testing. |
| **Password & PIN Hashing** | [PasswordHasher.cs](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi/Services/PasswordHasher.cs) | Mengapa tidak boleh menggunakan MD5/SHA1 atau simpan plaintext? Jelaskan peran **PBKDF2 SHA256**, **Salt**, dan **Iterations (100.000)** dalam mencegah *Rainbow Table* & *Brute Force*. |
| **JWT Authentication** | [AuthService.cs](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi/Services/AuthService.cs) | Bagaimana Token JWT dibentuk? Jelaskan elemen **Claims** (Email, Role, NIK), **Header**, **Payload**, dan **Signature (HMAC-SHA256)**. |
| **Otorisasi RBAC** | [AccountsController.cs](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi/Controllers/AccountsController.cs) | Penggunaan atribut `[Authorize(Roles = "Admin,Teller")]` vs `[Authorize(Roles = "Customer")]`. |
| **Input Validation** | [TransferRequestValidator.cs](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi/Validators/TransferRequestValidator.cs) | Mencegah *SQL Injection* & *MIME/Payload attack* menggunakan **FluentValidation** sebelum masuk ke Service Layer. |
| **Security Headers** | [SecurityHeadersMiddleware.cs](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi/Middleware/SecurityHeadersMiddleware.cs) | Penjelasan fungsi header HTTP: `HSTS`, `X-Frame-Options: DENY` (anti Clickjacking), `X-Content-Type-Options: nosniff`. |
| **Unit Testing** | [TransactionServiceTests.cs](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi.Tests/UnitTests/TransactionServiceTests.cs) | Konsep **AAA (Arrange, Act, Assert)**, isolasi pengujian logika bisnis transfer, mocking dependensi menggunakan **Moq**. |
| **Integration Testing** | [TransactionEndpointsTests.cs](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi.Tests/IntegrationTests/TransactionEndpointsTests.cs) | Pengujian HTTP End-to-End dengan **WebApplicationFactory** tanpa perlu mendeploy API ke server fisik. |

---

### Modul Pertemuan 9: Docker, CI/CD, & Capstone Hardening
| Topik Utama | Komponen Kode | Poin Penjelasan Kunci untuk Mahasiswa |
| :--- | :--- | :--- |
| **Multi-Stage Docker** | [Dockerfile](file:///d:/botcamp%20.net/pertemuan%206/Dockerfile) | Memisahkan *SDK Build Environment* (ukuran besar) dengan *Runtime Environment* (ukuran kecil & aman). |
| **Container Hardening** | [Dockerfile](file:///d:/botcamp%20.net/pertemuan%206/Dockerfile#L21-L24) | Menjalankan kontainer sebagai **non-root user (`appuser`)** agar jika terjadi eksploitasi, peretas tidak memiliki akses root ke host OS. |
| **Orkestrasi Service** | [docker-compose.yml](file:///d:/botcamp%20.net/pertemuan%206/docker-compose.yml) | Menghubungkan **API Service** dengan **PostgreSQL Database**, pengeset-an `healthcheck`, dan persistent volumes. |
| **CI/CD Automation** | [.github/workflows/ci-cd.yml](file:///d:/botcamp%20.net/pertemuan%206/.github/workflows/ci-cd.yml) | Tahapan pipeline otomatis: `Checkout` ➔ `Setup .NET` ➔ `Restore` ➔ `Build` ➔ `Test` ➔ `Docker Build`. |
| **Rate Limiting** | [Program.cs](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi/Program.cs#L82-L93) | Mencegah serangan *Distributed Denial of Service (DoS)* dan *Brute Force PIN* dengan `FixedWindowLimiter`. |
| **Exception Hardening** | [ExceptionHandlingMiddleware.cs](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi/Middleware/ExceptionHandlingMiddleware.cs) | Menyembunyikan stack trace database di Production menggunakan standar **RFC 7807 (ProblemDetails)**. |

---

## 🔍 2. Penjelasan Detail Setiap Kode (Line-by-Line Breakdown)

### A. Password & PIN Hashing ([PasswordHasher.cs](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi/Services/PasswordHasher.cs))
```csharp
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;      // 128-bit Salt unik per user
    private const int KeySize = 32;       // 256-bit Hash output
    private const int Iterations = 100000; // 100.000 kali pengulangan PBKDF2
```
* **Penjelasan Mentor**:
  1. **Salt**: Mengapa kita butuh Salt? Salt adalah byte acak yang ditambahkan ke password sebelum di-hash. Jika dua nasabah menggunakan password yang sama (misal `Password123!`), nilai hash-nya akan berbeda karena Salt-nya unik. Ini menggagalkan serangan *Rainbow Table*.
  2. **Iterations (100.000)**: PBKDF2 sengaja dibuat agak lambat secara komputasi agar peretas tidak bisa mencoba jutaan password per detik (*Brute Force*).

---

### B. Otentikasi JWT ([AuthService.cs](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi/Services/AuthService.cs))
```csharp
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
    new Claim(ClaimTypes.Email, customer.Email),
    new Claim(ClaimTypes.Role, customer.Role),
    new Claim("Nik", customer.Nik)
};
```
* **Penjelasan Mentor**:
  - **Claims** adalah data identitas yang "ditempelkan" ke dalam Token JWT.
  - Ketika client mengirimkan header `Authorization: Bearer <token>`, ASP.NET Core secara otomatis mengekstrak `claims` ini menjadi objek `User` (`ClaimsPrincipal`) yang dapat diakses di Controller.

---

### C. Logika Bisnis Transfer & Mutasi Atomik ([TransactionService.cs](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi/Services/TransactionService.cs))
```csharp
// 5. Cek Kecukupan Saldo (Overdraft Protection)
if (sourceAccount.Balance < request.Amount)
    throw new InvalidOperationException("Saldo tidak mencukupi untuk melakukan transfer.");

// 6. Eksekusi Mutasi Saldo Atomik
sourceAccount.Balance -= request.Amount;
targetAccount.Balance += request.Amount;
```
* **Penjelasan Mentor**:
  - **Overdraft Protection**: Memastikan saldo tidak menjadi minus.
  - **Transaksi Atomik**: Perubahan pada `sourceAccount` dan `targetAccount` terjadi di dalam satu siklus DbContext. Jika panggilan `_dbContext.SaveChangesAsync()` gagal, EF Core akan melakukan *rollback* otomatis sehingga saldo nasabah tidak terpotong sepihak.

---

### D. Unit Testing Terisolasi dengan Moq ([TransactionServiceTests.cs](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi.Tests/UnitTests/TransactionServiceTests.cs))
```csharp
_mockHasher.Setup(h => h.Verify("123456", "hashed_123456")).Returns(true);
```
* **Penjelasan Mentor**:
  - Di Unit Test, kita **tidak** ingin menguji algoritma hashing nyata (karena butuh waktu komputasi 100k iterasi).
  - Menggunakan **Moq**, kita memalsukan (*mock*) perilaku `IPasswordHasher` sehingga fokus tes murni pada logika pengurangan saldo dan pencatatan ledger transaksi.

---

### E. Integration Testing dengan WebApplicationFactory ([TransactionEndpointsTests.cs](file:///d:/botcamp%20.net/pertemuan%206/BankCoreApi.Tests/IntegrationTests/TransactionEndpointsTests.cs))
```csharp
_client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", auth1.Token);
```
* **Penjelasan Mentor**:
  - `WebApplicationFactory` membuat in-memory HTTP Server yang menjalankan selutuh pipeline ASP.NET Core (Middleware, Auth, Routing, Validation).
  - Kita menguji skenario nyata: Menguji apakah endpoint `/api/transactions/transfer` menolak request tanpa Token (`401 Unauthorized`) dan menerima request dengan Token yang sah (`200 OK`).

---

### F. Multi-Stage Dockerfile Security ([Dockerfile](file:///d:/botcamp%20.net/pertemuan%206/Dockerfile))
```dockerfile
# Stage 1: Build (Ukuran besar, ada SDK)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
...
# Stage 2: Final Runtime (Ukuran kecil, hanya ASP.NET Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
...
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
```
* **Penjelasan Mentor**:
  1. **Multi-Stage Build**: Menghasilkan kontainer produksi berukuran ringkas (~150MB dibanding ~800MB jika menyertakan SDK).
  2. **Non-Root User (`USER appuser`)**: Mengikuti standar *Zero Trust Security* perbankan.

---

### G. Kelanjutan Hasil Build CI/CD ([ci-cd.yml](file:///d:/botcamp%20.net/pertemuan%206/.github/workflows/ci-cd.yml))
* **Penjelasan Mentor**:
  1. **Upload ke Container Registry**: Docker Image hasil build yang dinyatakan **LULUS TES** di-push ke tempat penyimpanan image (seperti *Docker Hub* / *GitHub Container Registry*).
  2. **Automated Server Deployment (CD)**: Pipeline mengirim perintah ke Server Cloud / Kubernetes untuk menarik (*pull*) image baru dan melakukan *zero-downtime restart*.
  3. **Feedback Loop**: Jika ada test gagal, pipeline berhenti total dan memberi notifikasi ke developer (mencegah bug masuk ke Production).

---

## 🧪 3. Cara Memperagakan Pengujian & Running Proyek saat Mengajar

1. **Jalankan Unit & Integration Test via Terminal**:
   ```bash
   dotnet test
   ```
   Tunjukkan kepada mahasiswa bahwa 12 pengujian lulus 100% (*Passed: 12*).

2. **Jalankan Aplikasi dan Buka Swagger UI**:
   ```bash
   dotnet run --project BankCoreApi
   ```
   Buka browser di `http://localhost:5000/swagger`. Peragakan:
   - Login menggunakan akun seeded `admin@bankcore.com` / `AdminPass123!`.
   - Salin Token JWT yang dihasilkan.
   - Klik tombol **Authorize** di Swagger, ketik `Bearer <token_anda>`, lalu eksekusi endpoint terproteksi.

3. **Demonstrasikan Docker Compose**:
   ```bash
   docker-compose up --build
   ```
   Peragakan dua kontainer berjalan selaras (`bank_core_api` dan `bank_mssql_db`).

---
*Panduan Mentor ini dirancang agar siap pakai untuk penyampaian materi kelas yang interaktif dan berstandar industri.*
