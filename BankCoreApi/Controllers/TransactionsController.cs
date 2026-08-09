using BankCoreApi.Dtos;
using BankCoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankCoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Proteksi JWT Authorization
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    /// <summary>
    /// Transfer Dana Antar Rekening (Customer & Teller)
    /// </summary>
    [HttpPost("transfer")]
    [Authorize(Roles = "Customer,Teller")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        var result = await _transactionService.TransferAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Setor Tunai (Teller & Admin)
    /// </summary>
    [HttpPost("deposit")]
    [Authorize(Roles = "Teller,Admin")]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
    {
        var result = await _transactionService.DepositAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Tarik Tunai (Customer & Teller)
    /// </summary>
    [HttpPost("withdraw")]
    [Authorize(Roles = "Customer,Teller")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawalRequest request)
    {
        var result = await _transactionService.WithdrawalAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Cetak Rekening Koran / Statement Mutasi (Customer, Teller, Auditor, Admin)
    /// </summary>
    [HttpGet("statement/{accountNumber}")]
    [Authorize(Roles = "Customer,Teller,Auditor,Admin")]
    public async Task<IActionResult> GetStatement(string accountNumber)
    {
        var results = await _transactionService.GetStatementAsync(accountNumber);
        return Ok(results);
    }
}
