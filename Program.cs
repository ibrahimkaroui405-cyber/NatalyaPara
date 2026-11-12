using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PharmaTrust.Data;
using PharmaTrust.Services;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Ensure absolute path for SQLite database in App_Data
var contentRoot = builder.Environment.ContentRootPath;
var defaultDbPath = Path.Combine(contentRoot, "App_Data", "natalyapara.db");
var rawConn = builder.Configuration.GetConnectionString("DefaultConnection");
string connectionString;
if (string.IsNullOrEmpty(rawConn))
{
    connectionString = $"Data Source={defaultDbPath}";
}
else
{
    var rawPath = rawConn.Replace("Data Source=", "").Trim();
    var fullPath = Path.IsPathRooted(rawPath) ? rawPath : Path.Combine(contentRoot, rawPath);
    connectionString = $"Data Source={fullPath}";
}

var dbFilePath = connectionString.Replace("Data Source=", "").Trim();
var dbDir = Path.GetDirectoryName(dbFilePath);
if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
{
    Directory.CreateDirectory(dbDir);
}

// ----------------------------------------------------
// Structured Logging (Serilog) with Rolling Daily File
// ----------------------------------------------------
var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

try
{
    var logsDirectory = Path.Combine(contentRoot, "App_Data", "logs");
    if (!Directory.Exists(logsDirectory))
    {
        Directory.CreateDirectory(logsDirectory);
    }

    loggerConfig.WriteTo.File(
        path: Path.Combine(logsDirectory, "natalya-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        rollOnFileSizeLimit: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}");
}
catch (Exception ex)
{
    Console.WriteLine($"[Serilog Warning] Logs fichiers désactivés ou dossier inaccessible: {ex.Message}");
}

Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Authentication & Authorization Services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.AccessDeniedPath = "/Admin/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "NatalyaPara.AdminAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

// Rate Limiting Services (Protection against brute-force, scraping, and order spam)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.Append("Retry-After", "60");

        var isAjax = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                     || context.HttpContext.Request.Headers.Accept.ToString().Contains("application/json");

        if (isAjax)
        {
            context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
            await context.HttpContext.Response.WriteAsync(
                "{\"success\":false,\"message\":\"Trop de requêtes. Veuillez patienter une minute avant de réessayer.\"}", 
                token);
        }
        else
        {
            context.HttpContext.Response.ContentType = "text/html; charset=utf-8";
            await context.HttpContext.Response.WriteAsync(
                "<!DOCTYPE html><html lang='fr'><head><meta charset='utf-8'><title>Trop de requêtes - Natalya Para</title>" +
                "<meta name='viewport' content='width=device-width, initial-scale=1.0'>" +
                "<style>body{font-family:system-ui,-apple-system,sans-serif;display:flex;align-items:center;justify-content:center;min-height:100vh;margin:0;background:#faf7f5;color:#2d2926;text-align:center;padding:20px;box-sizing:border-box;}" +
                ".box{background:#fff;padding:40px;border-radius:16px;box-shadow:0 10px 25px rgba(0,0,0,0.06);max-width:480px;width:100%;}" +
                "h1{color:#8b5e3c;font-size:24px;margin-bottom:12px;}" +
                "p{font-size:15px;line-height:1.6;color:#666;margin-bottom:24px;}" +
                "a{display:inline-block;padding:12px 24px;background:#8b5e3c;color:#fff;border-radius:8px;text-decoration:none;font-weight:600;}</style></head>" +
                "<body><div class='box'><h1>Trop de requêtes (429)</h1>" +
                "<p>Pour votre sécurité et la fluidité de la boutique, les requêtes trop fréquentes ont été temporairement ralenties. Veuillez patienter environ 1 minute avant de réessayer.</p>" +
                "<a href='/'>Retour à la boutique</a></div></body></html>",
                token);
        }
    };

    // Global limiter across the entire site: 200 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetSlidingWindowLimiter(clientIp, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 200,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4,
            QueueLimit = 0
        });
    });

    // 1. Strict Brute-Force Auth Limiter: 5 login attempts per minute per IP
    options.AddPolicy("auth", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });

    // 2. Checkout Limiter: 5 order submissions per minute per IP
    options.AddPolicy("checkout", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });

    // 3. Cart API Limiter: 40 operations per minute per IP
    options.AddPolicy("cart-api", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetSlidingWindowLimiter(clientIp, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 40,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4,
            QueueLimit = 0
        });
    });
});

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITelegramService, TelegramService>();

var app = builder.Build();

// Initialize and Seed Database
try
{
    DbInitializer.Initialize(app.Services);
}
catch (Exception ex)
{
    Log.Error(ex, "Attention: Initialisation automatique de la base de données contournée pour garantir le démarrage");
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler("/Home/Error");
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0} ms";
});

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

try
{
    Log.Information("Démarrage du serveur Natalya Para...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Arrêt inattendu du serveur Natalya Para");
}
finally
{
    Log.CloseAndFlush();
}
