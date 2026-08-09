namespace BankCoreApi.Middleware;

/// <summary>
/// Middleware Pengaplikasian Security Headers HTTP (Pertemuan 7 - Web Security Hardening)
/// Memproteksi aplikasi dari Clickjacking, MIME Sniffing, dan Cross-Site Scripting (XSS).
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent Clickjacking attacks
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // Prevent MIME type sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Enable XSS Filtering in legacy browsers
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Enforce HTTPS-only connections via HSTS
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        // Restrict resource loading domains via CSP
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none';";

        await _next(context);
    }
}
