using BankCoreApi.Dtos;

namespace BankCoreApi.Services;

/// <summary>
/// Interface Service Autentikasi dan Otorisasi (JWT & User Claims)
/// </summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterCustomerRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
