using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace BankCoreApi.Middleware;

/// <summary>
/// Middleware Penanganan Error Terpusat / Global Exception Handler (Pertemuan 9 - Hardening RFC 7807)
/// Mengubah Exception menjadi respon ProblemDetails tanpa membocorkan Stack Trace sensitif di lingkungan Production.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Terjadi kesalahan tidak terduga dalam sistem perbankan: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title) = exception switch
        {
            KeyNotFoundException => (HttpStatusCode.NotFound, "Data Tidak Ditemukan"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Akses Tidak Diizinkan / Kredensial Salah"),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Operasi Transaksi Tidak Valid"),
            ArgumentException => (HttpStatusCode.BadRequest, "Argumen Request Tidak Sesuai"),
            _ => (HttpStatusCode.InternalServerError, "Kesalahan Internal Server Perbankan")
        };

        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        // Sertakan trace detail HANYA di environment Development
        if (_env.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        var json = JsonSerializer.Serialize(problemDetails);
        return context.Response.WriteAsync(json);
    }
}
