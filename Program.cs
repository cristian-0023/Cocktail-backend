using Cocktail.back.Data;
using Cocktail.back.Repositories;
using Cocktail.back.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;

// ================= BLINDAJE UTC ACTIVO =================
// Se maneja vía ApplicationDbContext y un switch global para Npgsql
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine("--- BACKEND STARTING: SECURE MODE ---");

// 1. Services Configuration
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Database Configuration
var connectionString = "";

// Detection of DATABASE_URL (for Cloud Deployment like Render/Railway)
var rawDatabaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") 
                   ?? Environment.GetEnvironmentVariable("database_url");

if (!string.IsNullOrEmpty(rawDatabaseUrl) && rawDatabaseUrl.Contains("://"))
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
        Console.WriteLine("--- DATABASE CONFIG: Connected to PostgreSQL cloud instance ---");
    }
    catch 
    {
        connectionString = rawDatabaseUrl; 
    }
}
else 
{
    // Fallback to appsettings.json
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
    Console.WriteLine("--- DATABASE CONFIG: Using local connection string ---");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (connectionString.Contains("Host=") || connectionString.Contains("postgres"))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// 3. DI Registration
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// 4. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://granizados-two.vercel.app", "http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
            
        policy.SetIsOriginAllowed(origin => 
        {
            if (string.IsNullOrEmpty(origin)) return false;
            var host = new Uri(origin).Host;
            return host == "localhost" || host == "127.0.0.1" || host.EndsWith(".vercel.app");
        });
    });
});

// 5. Auth
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "fallback_secret_key_long_enough_32_chars"))
        };
    });

var app = builder.Build();

// 6. Middleware
app.UseForwardedHeaders();
app.UseCors("AllowFrontend");

// Global Exception Handler
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (Exception ex)
    {
        Console.WriteLine($"[CRITICAL] {ex.Message}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "Internal Error", message = ex.Message });
    }
});

app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", "API V1"); c.RoutePrefix = "swagger"; });

if (!app.Environment.IsProduction()) app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 7. Auto DB Init
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try 
    { 
        context.Database.EnsureCreated(); 
        Console.WriteLine("--- SCHEMA READY ---");
    }
    catch (Exception ex) { Console.WriteLine($"DB INIT ERROR: {ex.Message}"); }
}

app.Run();
