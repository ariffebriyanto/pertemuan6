using System.Security.Claims;
using BankCoreApi.Dtos;
using BankCoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankCoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Semua endpoint wajib membawa valid JWT Token
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Pembukaan Rekening Baru (Otorisasi: Admin & Teller)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Teller")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var result = await _accountService.CreateAccountAsync(request);
        return CreatedAtAction(nameof(GetAccount), new { accountNumber = result.AccountNumber }, result);
    }

    /// <summary>
    /// Ambil Informasi Rekening berdasarkan Nomor Rekening
    /// </summary>
    [HttpGet("{accountNumber}")]
    [Authorize(Roles = "Customer,Teller,Auditor,Admin")]
    public async Task<IActionResult> GetAccount(string accountNumber)
    {
        var result = await _accountService.GetAccountByNumberAsync(accountNumber);
        return Ok(result);
    }

    /// <summary>
    /// Ambil Daftar Rekening Milik Nasabah yang Sedang Login (Otorisasi: Customer)
    /// </summary>
    [HttpGet("my-accounts")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyAccounts()
    {
        var customerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId))
            return Unauthorized("Identitas Token JWT tidak valid.");

        var results = await _accountService.GetCustomerAccountsAsync(customerId);
        return Ok(results);
    }
}
