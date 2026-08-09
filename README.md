# Core Banking RESTful API (.NET 9 C#) 🏦

Aplikasi acuan / materi pembelajaran komprehensif untuk **Pertemuan 7** dan **Pertemuan 9** Bootcamp .NET. Berisi implementasi utuh **Digital Core Banking System** dengan standar keamanan industri, pengujian otomatis, kontainerisasi Docker, dan pipeline CI/CD.

---

## 📚 Ringkasan Materi Pembelajaran

### 🔐 Pertemuan 7 — Security, Authentication/Authorization, & Automated Testing
1. **Authentication & JWT Claims**: Implementasi penandatanganan dan verifikasi JWT Bearer Token dengan Claims (UserId, Email, FullName, Role, NIK).
2. **Role-Based Access Control (RBAC)**: Otorisasi berbasis Role (`Customer`, `Teller`, `Auditor`, `Admin`) pada setiap Controller Endpoint.
3. **Cryptographic Password & PIN Hashing**: Penggunaan algoritma **PBKDF2 SHA256** dengan Salt acak untuk meng-hash password nasabah dan 6-digit PIN transaksi keuangan.
4. **Input Validation & Sanitization**: Validasi DTO berbasis **FluentValidation** untuk mencegah SQL Injection & Cross-Site Scripting (XSS).
5. **Web Security Headers**: Middleware penambahan header HTTP (`HSTS`, `Content-Security-Policy`, `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`).
6. **Automated Testing Suite**:
   - **Unit Testing**: Pengujian terisolasi Service Layer (`AuthService`, `TransactionService`, `PasswordHasher`) menggunakan **xUnit** dan **Moq**.
   - **Integration Testing**: Pengujian End-to-End API menggunakan `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) dan database SQLite In-Memory.

### 🐳 Pertemuan 9 — Docker, CI/CD, & Capstone Hardening
1. **Multi-Stage Dockerfile**: Optimalisasi image kontainer .NET 9 dengan pertahanan keamanan (menjalankan kontainer sebagai non-root user `appuser`).
2. **Docker Compose**: Orchestration API Service dan **PostgreSQL 16** database container lengkap dengan persistent volume dan health check probes.
3. **CI/CD Pipeline**: GitHub Actions (`.github/workflows/ci-cd.yml`) otomatisasi `restore`, `build`, `test`, dan `docker build`.
4. **Capstone Hardening**:
   - **Rate Limiting**: ASP.NET Core `FixedWindowLimiter` (mencegah Brute Force PIN/Password & DoS attack).
   - **Health Checks**: Endpoint `/healthz` untuk probe status kesehatan aplikasi dan basis data.
   - **Global Exception Middleware**: Standardisasi error response **RFC 7807 (ProblemDetails)** tanpa membocorkan stack trace internal.
   - **Swagger Security**: Dokumentasi Swagger OpenAPI yang terintegrasi dengan skema JWT Bearer Authentication.

---

## 🛠️ Cara Menjalankan Aplikasi

### 1. Menjalankan via .NET CLI
```bash
# Clone atau buka direktori proyek
cd "d:\botcamp .net\pertemuan 6"

# Build Solusi
dotnet build

# Menjalankan Pengujian Otomatis (Unit & Integration Tests)
dotnet test

# Menjalankan Aplikasi Web API
dotnet run --project BankCoreApi
```
- Dokumentasi Swagger UI dapat diakses di: `http://localhost:5000/swagger` atau `https://localhost:5001/swagger`
- Health Check Probe: `http://localhost:5000/healthz`

### 2. Menjalankan via Docker Compose (Pertemuan 9)
```bash
# Build dan jalankan API & PostgreSQL Database
docker-compose up --build -d

# Memeriksa status log kontainer
docker-compose logs -f bank-api

# Menghentikan kontainer
docker-compose down
```

---

## 📑 Struktur Proyek

```
pertemuan 6/
├── BankCoreApi/                         # Application Web API (.NET 9)
│   ├── Controllers/                     # AuthController, AccountsController, TransactionsController, HealthController
│   ├── Models/                          # Customer, Account, Transaction entities
│   ├── Dtos/                            # AuthDtos, AccountDtos, TransactionDtos
│   ├── Services/                        # AuthService, AccountService, TransactionService, PasswordHasher
│   ├── Validators/                      # FluentValidation rules
│   ├── Middleware/                      # SecurityHeadersMiddleware, ExceptionHandlingMiddleware
│   └── Data/                            # BankDbContext & EF Core mapping
├── BankCoreApi.Tests/                  # Automated Test Project (xUnit)
│   ├── UnitTests/                       # PasswordHasherTests, TransactionServiceTests (Moq)
│   └── IntegrationTests/                # CustomWebApplicationFactory, AuthEndpointsTests, TransactionEndpointsTests
├── Dockerfile                           # Multi-Stage Production Dockerfile
├── docker-compose.yml                   # Docker Compose setup (API + PostgreSQL)
├── .github/workflows/ci-cd.yml          # GitHub Actions Pipeline
├── README.md                            # Dokumentasi Umum
└── GUIDE_MENTOR.md                      # Panduan Mengajar Mentor (Penjelasan Detail Kode dalam Bahasa Indonesia)
```
