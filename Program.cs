using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PharmaTrust.Data;
using PharmaTrust.Services;

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

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

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
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.Name = "NatalyaPara.AdminAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Initialize and Seed Database
DbInitializer.Initialize(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
