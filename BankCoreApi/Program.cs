using System.Text;
using System.Threading.RateLimiting;
using BankCoreApi.Data;
using BankCoreApi.Middleware;
using BankCoreApi.Models;
using BankCoreApi.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<BankDbContext>(options =>
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.UseInMemoryDatabase("BankCoreDb");
    }
    else if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseInMemoryDatabase("BankCoreDb");
    }
});

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

// Registrasi FluentValidation (Pertemuan 7 - Input Validation)
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ==========================================
// 2. CONFIG KEAMANAN SWAGGER (JWT BEARER UI)
// ==========================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BankCoreApi - Core Banking RESTful API",
        Version = "v1",
        Description = "Sistem Core Banking API - Pertemuan 7 (Security & Testing) & Pertemuan 9 (Docker, CI/CD, Hardening)"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Masukkan Token JWT dengan format: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ==========================================
// 3. KONFIGURASI JWT AUTHENTICATION (Pertemuan 7)
// ==========================================
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SuperSecretBankingKey2026!MustBeLongEnoughForHS256Algorithm";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BankCoreApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BankCoreClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ==========================================
// 4. RATE LIMITING (Pertemuan 9 - Hardening)
// ==========================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("FinancialRatePolicy", policy =>
    {
        policy.PermitLimit = 10; // Max 10 request per window
        policy.Window = TimeSpan.FromSeconds(10);
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit = 2;
    });
});

var app = builder.Build();

// ==========================================
// 5. MIDDLEWARE PIPELINE EXECUTION
// ==========================================
// Middleware Keamanan Pertemuan 7 & 9
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Aktifkan Swagger UI untuk pengujian API (Development & Docker Demo)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BankCoreApi v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ==========================================
// 6. SEEDING INITIAL BANKING DATA
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    // Buat tabel & skema otomatis jika menggunakan SQL Server
    db.Database.EnsureCreated();

    if (!db.Customers.Any())
    {
        var adminCustomer = new Customer
        {
            Nik = "3171000000000001",
            FullName = "Administrator Bank",
            Email = "admin@bankcore.com",
            PasswordHash = hasher.Hash("AdminPass123!"),
            PinHash = hasher.Hash("123456"),
            Role = "Admin"
        };

        var sampleCustomer = new Customer
        {
            Nik = "3171000000000002",
            FullName = "Budi Santoso",
            Email = "budi@gmail.com",
            PasswordHash = hasher.Hash("BudiPass123!"),
            PinHash = hasher.Hash("654321"),
            Role = "Customer"
        };

        db.Customers.AddRange(adminCustomer, sampleCustomer);
        db.SaveChanges();

        var account1 = new Account
        {
            AccountNumber = "1001234567",
            CustomerId = sampleCustomer.Id,
            Balance = 5000000m,
            AccountType = "Savings"
        };

        db.Accounts.Add(account1);
        db.SaveChanges();
    }
}

app.Run();

// Dideklarasikan agar WebApplicationFactory pada Integration Tests dapat mengakses kelas Program
public partial class Program { }
