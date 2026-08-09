using System.Net;
using System.Net.Http.Json;
using BankCoreApi.Dtos;
using FluentAssertions;
using Xunit;

namespace BankCoreApi.Tests.IntegrationTests;

/// <summary>
/// Integration Tests untuk Endpoint Autentikasi (/api/auth) (Pertemuan 7 - Pengujian Integrasi Otomatis)
/// Menguji respon HTTP nyata dari Controller Registrasi dan Login menggunakan WebApplicationFactory.
/// </summary>
public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        // _client bertindak sebagai HTTP Client virtual untuk mengirim request HTTP ke API
        _client = factory.CreateClient();
    }

    /// <summary>
    /// TEST 1: Skenario Registrasi Nasabah Baru Sukses
    /// Memastikan HTTP POST ke /api/auth/register mengembalikan status 200 OK dan Token JWT.
    /// </summary>
    [Fact]
    public async Task Register_ShouldReturnSuccessAndJwtToken_WhenRequestIsValid()
    {
        // === 1. ARRANGE (Data Registrasi) ===
        var request = new RegisterCustomerRequest(
            Nik: "3515000000000001",
            FullName: "Siti Rahma",
            Email: "siti@gmail.com",
            Password: "Password123!",
            Pin: "123456"
        );

        // === 2. ACT (Kirim Request HTTP POST ke Endpoint /api/auth/register) ===
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // === 3. ASSERT (Verifikasi Status Respon HTTP & Payload JWT) ===
        // Memastikan HTTP Response Status Code adalah 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK, "karena data registrasi valid dan NIK belum terdaftar");

        // De-serialisasi JSON response menjadi DTO AuthResponse
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        
        // Memastikan Token JWT diterbitkan dan email sesuai
        result!.Token.Should().NotBeNullOrEmpty("karena JWT Token wajib dihasilkan setelah registrasi sukses");
        result.Email.Should().Be("siti@gmail.com");
    }

    /// <summary>
    /// TEST 2: Skenario Login Kredensial Salah
    /// Memastikan HTTP POST ke /api/auth/login menolak dengan HTTP 401 Unauthorized jika email/password salah.
    /// </summary>
    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        // === 1. ARRANGE (Kredensial Asal/Salah) ===
        var request = new LoginRequest("nonexistent@gmail.com", "WrongPassword");

        // === 2. ACT (Kirim Request HTTP POST ke /api/auth/login) ===
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // === 3. ASSERT (Verifikasi Keamanan) ===
        // Memastikan API merespon dengan status 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "karena akun tidak ditemukan di database");
    }
}
