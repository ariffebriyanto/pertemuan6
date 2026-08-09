using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BankCoreApi.Data;
using BankCoreApi.Dtos;
using BankCoreApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BankCoreApi.Services;

/// <summary>
/// Service Implementasi Autentikasi Pengguna & Penerbitan JWT Token (Pertemuan 7 - Security & Auth)
/// </summary>
public class AuthService : IAuthService
{
    private readonly BankDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(BankDbContext dbContext, IPasswordHasher passwordHasher, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterCustomerRequest request)
    {
        // 1. Cek Duplikasi NIK atau Email
        if (await _dbContext.Customers.AnyAsync(c => c.Nik == request.Nik))
            throw new InvalidOperationException("NIK sudah terdaftar dalam sistem perbankan.");

        if (await _dbContext.Customers.AnyAsync(c => c.Email == request.Email))
            throw new InvalidOperationException("Email sudah terdaftar.");

        // 2. Hash Password dan PIN demi Keamanan Data Perbankan
        var customer = new Customer
        {
            Nik = request.Nik,
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            PinHash = _passwordHasher.Hash(request.Pin),
            Role = string.IsNullOrWhiteSpace(request.Role) ? "Customer" : request.Role
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        // 3. Generate JWT Token setelah registrasi sukses
        return GenerateJwtToken(customer);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // 1. Cari Nasabah berdasarkan Email
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Email == request.Email);

        if (customer == null || !_passwordHasher.Verify(request.Password, customer.PasswordHash))
        {
            throw new UnauthorizedAccessException("Email atau Password salah.");
        }

        // 2. Penerbitan JWT Token jika kredensial valid
        return GenerateJwtToken(customer);
    }

    private AuthResponse GenerateJwtToken(Customer customer)
    {
        var secretKey = _configuration["Jwt:Secret"] ?? "SuperSecretBankingKey2026!MustBeLongEnoughForHS256Algorithm";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims: Menyimpan identitas pengguna dalam Token terenkripsi
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
            new Claim(ClaimTypes.Email, customer.Email),
            new Claim(ClaimTypes.Name, customer.FullName),
            new Claim(ClaimTypes.Role, customer.Role),
            new Claim("Nik", customer.Nik)
        };

        var expiresAt = DateTime.UtcNow.AddHours(2); // Token berlaku 2 Jam

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "BankCoreApi",
            audience: _configuration["Jwt:Audience"] ?? "BankCoreClients",
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponse(
            Token: tokenString,
            ExpiresAt: expiresAt,
            FullName: customer.FullName,
            Email: customer.Email,
            Role: customer.Role
        );
    }
}
