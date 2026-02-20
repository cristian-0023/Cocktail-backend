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

// Forwarded Headers (Railway / proxies Linux)
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

// ================== DATABASE ==================
// 1. Detección agresiva y sensible a mayúsculas/minúsculas de DATABASE_URL
var envVars = Environment.GetEnvironmentVariables();
string? rawDatabaseUrl = null;

// Buscamos en orden de prioridad
string[] searchKeys = { "DATABASE_URL", "database_url", "POSTGRES_URL", "POSTGRESQL_URL" };
foreach (var key in searchKeys)
{
    if (envVars.Contains(key))
    {
        var val = envVars[key]?.ToString();
        // Evitamos que use el placeholder literal "${DATABASE_URL}" si viene de un archivo corrupto
        if (!string.IsNullOrEmpty(val) && !val.Contains("${"))
        {
            rawDatabaseUrl = val;
            break;
        }
    }
}

var connectionString = "";
var databaseProvider = "SqlServer"; // Default

if (!string.IsNullOrEmpty(rawDatabaseUrl))
{
    databaseProvider = "PostgreSQL";
    Console.WriteLine($"--- DATABASE CONFIG: Connection source found ({rawDatabaseUrl.Split(':')[0]}://...) ---");

    if (rawDatabaseUrl.Contains("://"))
    {
        try 
        {
            var uri = new Uri(rawDatabaseUrl);
            var userInfoParts = uri.UserInfo.Split(':', 2);
            var user = Uri.UnescapeDataString(userInfoParts[0]);
            var password = userInfoParts.Length > 1 ? Uri.UnescapeDataString(userInfoParts[1]) : "";
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.Trim('/');

            connectionString = $"Host={uri.Host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=True;Pooling=true;";
            Console.WriteLine("--- Connection successfully parsed from URI ---");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CRITICAL ERROR PARSING URI: {ex.Message}");
            connectionString = rawDatabaseUrl; // Fallback al original
        }
    }
    else
    {
        connectionString = rawDatabaseUrl;
    }
}
else 
{
    // Fallback: Variables individuales o AppSettings
    var pgHost = Environment.GetEnvironmentVariable("PGHOST") ?? Environment.GetEnvironmentVariable("POSTGRES_HOST");
    var pgDb = Environment.GetEnvironmentVariable("PGDATABASE") ?? Environment.GetEnvironmentVariable("POSTGRES_DB");
    
    if (!string.IsNullOrEmpty(pgHost) && !string.IsNullOrEmpty(pgDb))
    {
        databaseProvider = "PostgreSQL";
        var pgUser = Environment.GetEnvironmentVariable("PGUSER") ?? Environment.GetEnvironmentVariable("POSTGRES_USER");
        var pgPass = Environment.GetEnvironmentVariable("PGPASSWORD") ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
        var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";

        connectionString = $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPass};SSL Mode=Require;Trust Server Certificate=True;Pooling=true;";
        Console.WriteLine("--- DATABASE CONFIG: Parsed from individual environment variables ---");
    }
    else 
    {
        // Intentamos leer de la configuración (appsettings.json)
        var configCs = builder.Configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(configCs) && !configCs.Contains("${"))
        {
            connectionString = configCs;
            databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";
            Console.WriteLine("--- DATABASE CONFIG: Using configuration file ---");
        }
        else 
        {
            Console.WriteLine("WARNING: No valid database connection string found!");
        }
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Forzamos Npgsql si detectamos parámetros de PostgreSQL o si el string parece ser de Postgres
    bool isPostgres = databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || 
                     (!string.IsNullOrEmpty(connectionString) && (connectionString.Contains("Host=") || connectionString.Contains("postgres")));

    if (isPostgres)
    {
        // Asegurar SSL Mode para Railway/Nube
        if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("SSL Mode="))
        {
            connectionString = connectionString.TrimEnd(';') + ";SSL Mode=Require;Trust Server Certificate=True;";
        }
        
        options.UseNpgsql(connectionString);
        Console.WriteLine("DbContext initialized with Npgsql provider");
    }
    else
    {
        options.UseSqlServer(connectionString);
        Console.WriteLine("DbContext initialized with SqlServer provider");
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

// ================== CORS ==================
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
        policy.WithOrigins(allowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowedToAllowWildcardSubdomains();
            
        // Fallback dinámico para subdominios de Vercel y localhost
        policy.SetIsOriginAllowed(origin => 
        {
            if (string.IsNullOrEmpty(origin)) return false;
            var host = new Uri(origin).Host;
            return host == "localhost" || 
                   host == "127.0.0.1" || 
                   host.EndsWith(".vercel.app");
        });
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
                    builder.Configuration["Jwt:Key"] ??
                    "fallback_secret_key_at_least_32_characters_long"
                )
            )
        };
    });

var app = builder.Build();

// ================== MIDDLEWARE ==================

app.UseForwardedHeaders();

// CORS antes de auth
app.UseCors("AllowFrontend");

// Global error handler
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
        context.Response.ContentType = "application/json";

        var errorMessage = ex.Message;
        if (ex.InnerException != null) {
            errorMessage += " | Inner: " + ex.InnerException.Message;
        }

        await context.Response.WriteAsJsonAsync(new
        {
            error = "Internal Server Error",
            message = errorMessage,
            path = context.Request.Path.Value
        });
    }
});

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "Cocktail API V1");
    c.RoutePrefix = "swagger"; // Swagger UI at /swagger
});

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// ================== DIAGNOSTIC ENDPOINT ==================
app.MapGet("/api/test-db", async (ApplicationDbContext context, IConfiguration config) =>
{
    string MaskPassword(string? str)
    {
        if (string.IsNullOrEmpty(str)) return "NULL/Empty";
        if (str.Contains("Password="))
        {
            var parts = str.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].StartsWith("Password=")) parts[i] = "Password=***";
            }
            return string.Join(";", parts);
        }
        return str;
    }

    try
    {
        var canConnect = await context.Database.CanConnectAsync();
        var productCount = await context.Products.CountAsync();
        
        var availableVariableNames = Environment.GetEnvironmentVariables().Keys
            .Cast<string>()
            .OrderBy(k => k)
            .ToList();

        return Results.Ok(new { 
            status = "Connected", 
            canConnect, 
            productCount,
            provider = context.Database.ProviderName,
            attemptedConnectionString = MaskPassword(context.Database.GetDbConnection().ConnectionString),
            availableVariableNames
        });
    }
    catch (Exception ex)
    {
        var availableVariableNames = Environment.GetEnvironmentVariables().Keys
            .Cast<string>()
            .OrderBy(k => k)
            .ToList();

        return Results.Problem(detail: ex.Message, title: "Database Connection Error", extensions: new Dictionary<string, object?> {
            { "attemptedConnectionString", MaskPassword(context.Database.GetDbConnection().ConnectionString) },
            { "availableVariableNames", availableVariableNames }
        });
    }
});

app.MapControllers();

// ================== AUTO DB INIT ==================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        context.Database.EnsureCreated();
        Console.WriteLine("Database initialized");

    }
    catch (Exception ex)
    {
        Console.WriteLine($"DATABASE ERROR: {ex.Message}");
    }

    // Crear carpeta uploads
    try
    {
        var uploadsFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            "products"
        );

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"UPLOAD ERROR: {ex.Message}");
    }
}

app.Run();
