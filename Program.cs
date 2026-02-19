using Cocktail.back.Data;
using Cocktail.back.Repositories;
using Cocktail.back.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ================== SERVICES ==================

// Forwarded Headers (Render / proxies Linux)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ================== DATABASE (Render Compatible) ==================
// 1. Obtener DATABASE_URL (Render usa esta variable por defecto)
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = "";
var databaseProvider = "SqlServer"; // Default fallback

Console.WriteLine($"--- DATABASE CONFIG START ---");

if (!string.IsNullOrEmpty(databaseUrl))
{
    databaseProvider = "PostgreSQL";
    Console.WriteLine("Detected DATABASE_URL environment variable.");

    try
    {
        // Parseo robusto para Render (postgres://user:password@host:port/database)
        var uri = new Uri(databaseUrl);
        var userInfoParts = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfoParts[0]);
        var password = userInfoParts.Length > 1 ? Uri.UnescapeDataString(userInfoParts[1]) : "";
        var dbPort = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.Trim('/');

        // Construir connection string de Npgsql con SSL
        connectionString = $"Host={uri.Host};Port={dbPort};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=True;Pooling=true;";
        
        Console.WriteLine($"Successfully parsed DATABASE_URL (Host: {uri.Host}, Port: {dbPort}, DB: {database})");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"CRITICAL ERROR PARSING DATABASE_URL: {ex.Message}");
        // Fallback: usar el string tal cual por si Npgsql puede manejarlo
        connectionString = databaseUrl;
    }
}
else
{
    // Fallback: Local development / AppSettings / Variables individuales
    Console.WriteLine("No DATABASE_URL found. Checking fallback configurations...");
    
    var pgHost = Environment.GetEnvironmentVariable("PGHOST") ?? Environment.GetEnvironmentVariable("POSTGRES_HOST");
    var pgDb = Environment.GetEnvironmentVariable("PGDATABASE") ?? Environment.GetEnvironmentVariable("POSTGRES_DB");

    if (!string.IsNullOrEmpty(pgHost) && !string.IsNullOrEmpty(pgDb))
    {
        databaseProvider = "PostgreSQL";
        var pgUser = Environment.GetEnvironmentVariable("PGUSER") ?? Environment.GetEnvironmentVariable("POSTGRES_USER");
        var pgPass = Environment.GetEnvironmentVariable("PGPASSWORD") ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
        var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";

        connectionString = $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPass};SSL Mode=Require;Trust Server Certificate=True;Pooling=true;";
        Console.WriteLine("Configured from individual PG environment variables.");
    }
    else
    {
        // Último recurso: appsettings.json (Desarrollo local)
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
        // Si la connection string de appsettings es para SQLExpress o similar
        if (connectionString.Contains("Server=") || connectionString.Contains("Data Source="))
        {
            databaseProvider = "SqlServer";
        }
        else if (connectionString.Contains("Host="))
        {
             databaseProvider = "PostgreSQL";
        }
        
        Console.WriteLine($"Using configuration file connection string (Provider: {databaseProvider})");
    }
}

// Configurar DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        // Asegurar SSL para PostgreSQL (Render lo requiere)
        if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("SSL Mode="))
        {
            connectionString += ";SSL Mode=Require;Trust Server Certificate=True;";
        }
        options.UseNpgsql(connectionString);
        Console.WriteLine("DbContext initialized with **Npgsql** (PostgreSQL)");
    }
    else
    {
        options.UseSqlServer(connectionString);
        Console.WriteLine("DbContext initialized with **SqlServer**");
    }
});

// ================== DEPENDENCY INJECTION ==================
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// ================== CORS (Render & Vercel) ==================
var frontendUrl = builder.Configuration["FRONTEND_URL"];
var allowedOrigins = new List<string> { 
    "https://granizados-two.vercel.app", 
    "http://localhost:5173",
    "http://127.0.0.1:5173"
};

if (!string.IsNullOrEmpty(frontendUrl)) 
{
    allowedOrigins.Add(frontendUrl);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => 
        {
            if (string.IsNullOrEmpty(origin)) return false;
            try 
            {
                var host = new Uri(origin).Host;
                
                // Permitir localhost, Vercel, y dominios de Render (*.onrender.com)
                return host == "localhost" || 
                       host == "127.0.0.1" || 
                       host.EndsWith(".vercel.app") ||
                       host.EndsWith(".onrender.com") ||
                       allowedOrigins.Contains(origin);
            }
            catch { return false; }
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// ================== JWT ==================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"] ?? "fallback_secret_key_at_least_32_characters_long"
                )
            )
        };
    });

var app = builder.Build();

// ================== MIDDLEWARE ==================
app.UseForwardedHeaders();

app.UseCors("AllowFrontend");

// Global exception handler
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[GLOBAL ERROR] {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cocktail API V1");
    c.RoutePrefix = string.Empty;
});

if (!app.Environment.IsProduction())
{
    // app.UseHttpsRedirection(); // Se suele activar en local, en Render el balanceador maneja SSL
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// ================== ROUTES ==================
app.MapControllers();

app.MapGet("/api/test-db", async (ApplicationDbContext context) =>
{
    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        return Results.Ok(new { status = "Connected", canConnect, provider = context.Database.ProviderName });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, title: "DB Connection Error");
    }
});

// ================== AUTO INIT & FOLDERS ==================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        Console.WriteLine("Database initialized/verified.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DATABASE ERROR: {ex.Message}");
    }

    // Crear carpeta uploads compatible con Linux
    try
    {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
            Console.WriteLine($"Created uploads folder at: {uploadsFolder}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"UPLOAD FOLDER ERROR: {ex.Message}");
    }
}

// ================== PORT BINDING (Render) ==================
// ================== PORT BINDING (Render) ==================
// Render inyecta la variable PORT. Si no existe, usamos 8080 o 5000.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
Console.WriteLine($"Starting application on port {port}...");

// IMPORTANTE: Para Render, usar Urls.Add antes de Run()
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
