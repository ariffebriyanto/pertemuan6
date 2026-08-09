namespace BankCoreApi.Dtos;

/// <summary>
/// DTO Request Registrasi Nasabah Baru
/// </summary>
public record RegisterCustomerRequest(
    string Nik,
    string FullName,
    string Email,
    string Password,
    string Pin,
    string Role = "Customer"
);

/// <summary>
/// DTO Request Login Pengguna
/// </summary>
public record LoginRequest(
    string Email,
    string Password
);

/// <summary>
/// DTO Response Autentikasi setelah Login / Registrasi Sukses
/// </summary>
public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    string FullName,
    string Email,
    string Role
);
