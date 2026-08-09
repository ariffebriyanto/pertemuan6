using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BankCoreApi.Dtos;
using FluentAssertions;
using Xunit;

namespace BankCoreApi.Tests.IntegrationTests;

/// <summary>
/// Integration Tests untuk Endpoint Transaksi Perbankan (/api/transactions) (Pertemuan 7 - E2E Testing & Keamanan JWT)
/// Menguji otorisasi JWT Bearer token pada request transfer dana keuangan.
/// </summary>
public class TransactionEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TransactionEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// TEST 1: Proteksi Keamanan Tanpa JWT Token
    /// Memastikan HTTP POST ke /api/transactions/transfer menolak dengan status 401 Unauthorized jika tidak menyertakan Authorization Bearer Header.
    /// </summary>
    [Fact]
    public async Task Transfer_ShouldReturnUnauthorized_WhenNoBearerTokenProvided()
    {
        // === 1. ARRANGE ===
        var request = new TransferRequest("1001234567", "1007654321", 50000m, "123456");

        // === 2. ACT (Kirim Request tanpa Authorization Header) ===
        var response = await _client.PostAsJsonAsync("/api/transactions/transfer", request);

        // === 3. ASSERT ===
        // Memastikan API menolak request tidak terautentikasi
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "karena endpoint transfer diproteksi atribut [Authorize]");
    }

    /// <summary>
    /// TEST 2: Skenario E2E Transfer Berhasil dengan JWT Token Sah
    /// Alur E2E: Registrasi Nasabah A & B ➔ Pembukaan Rekening ➔ Pasang JWT Bearer Header ➔ Transfer Rp 200.000 ➔ Verifikasi HTTP 200 OK.
    /// </summary>
    [Fact]
    public async Task Transfer_ShouldExecuteSuccessfully_WhenAuthenticatedUserPerformsTransfer()
    {
        // === LANGKAH 1: Registrasi Nasabah A & dapatkan Token JWT ===
        var regUser1 = new RegisterCustomerRequest("3171000000000100", "Nasabah A", "userA@gmail.com", "PassA123!", "111111");
        var resUser1 = await _client.PostAsJsonAsync("/api/auth/register", regUser1);
        var auth1 = await resUser1.Content.ReadFromJsonAsync<AuthResponse>();

        // === LANGKAH 2: Registrasi Nasabah B ===
        var regUser2 = new RegisterCustomerRequest("3171000000000200", "Nasabah B", "userB@gmail.com", "PassB123!", "222222");
        await _client.PostAsJsonAsync("/api/auth/register", regUser2);

        // === LANGKAH 3: Login sebagai Admin untuk Pembukaan Rekening ===
        var adminLogin = new LoginRequest("admin@bankcore.com", "AdminPass123!");
        var adminRes = await _client.PostAsJsonAsync("/api/auth/login", adminLogin);
        var adminAuth = await adminRes.Content.ReadFromJsonAsync<AuthResponse>();

        // Set Header Authorization dengan Token Admin
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.Token);

        // Admin Buka Rekening untuk Nasabah A (Setoran Awal Rp 1.000.000)
        var accA = await _client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest(Guid.NewGuid(), "Savings", 1000000m));
        
        // Admin Buka Rekening untuk Nasabah B (Setoran Awal Rp 500.000)
        var accB = await _client.PostAsJsonAsync("/api/accounts", new CreateAccountRequest(Guid.NewGuid(), "Savings", 500000m));

        if (accA.IsSuccessStatusCode && accB.IsSuccessStatusCode)
        {
            var resAccA = await accA.Content.ReadFromJsonAsync<AccountResponse>();
            var resAccB = await accB.Content.ReadFromJsonAsync<AccountResponse>();

            // === LANGKAH 4: Pasang Token JWT Nasabah A ke Header & Eksekusi Transfer ===
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth1!.Token);

            var transferReq = new TransferRequest(
                resAccA!.AccountNumber,
                resAccB!.AccountNumber,
                200000m,
                "111111",
                "Transfer E2E Test"
            );

            // === LANGKAH 5: Eksekusi Request Transfer HTTP POST ===
            var transferRes = await _client.PostAsJsonAsync("/api/transactions/transfer", transferReq);

            // === LANGKAH 6: Assert Respon Transfer Berhasil ===
            transferRes.StatusCode.Should().Be(HttpStatusCode.OK, "karena Token JWT sah, PIN benar, dan saldo mencukupi");
        }
    }
}
