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

// Configure Forwarded Headers (Railway / proxies Linux)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});



// Controllers + JSON config
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });



// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



// Database Context - Soporte para SQL Server (dev) y PostgreSQL (producción)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || 
        connectionString?.Contains("postgres") == true)
    {
        options.UseNpgsql(connectionString);
        Console.WriteLine("Using PostgreSQL database");
    }
    else
    {
        options.UseSqlServer(connectionString);
        Console.WriteLine("Using SQL Server database");
    }
});



// Dependency Injection
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();



// ================== CORS ==================
// Obtener URL del frontend desde variable de entorno o usar defaults
var frontendUrl = builder.Configuration.GetValue<string>("FRONTEND_URL");
var allowedOrigins = new List<string>
{
    // LOCAL
    "http://localhost:5173",
    "http://localhost:5174",
    "https://localhost:5173",
    "https://localhost:5174"
};

// Agregar URL de producción si está configurada
if (!string.IsNullOrEmpty(frontendUrl))
{
    allowedOrigins.Add(frontendUrl);
    Console.WriteLine($"CORS: Added production frontend URL: {frontendUrl}");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins.ToArray())
        .AllowAnyMethod()
        .AllowAnyHeader()
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
                    builder.Configuration["Jwt:Key"] ??
                    "fallback_secret_key_at_least_32_characters_long"
                )
            )
        };
    });



var app = builder.Build();



// ================== MIDDLEWARE ==================

// Forwarded headers (IMPORTANTE para Railway)
app.UseForwardedHeaders();



// CORS (ANTES de auth)
app.UseCors("AllowFrontend");



// Global Error Handler
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

        await context.Response.WriteAsJsonAsync(new
        {
            error = "Internal Server Error",
            message = ex.Message,
            path = context.Request.Path
        });
    }
});



// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cocktail API V1");
    c.RoutePrefix = "swagger";
});



app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();



// ================== AUTO DB INIT ==================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        Console.WriteLine("Initializing database schema...");
        context.Database.EnsureCreated();
        Console.WriteLine("Database initialized successfully.");

        var adminUser = context.Users
            .FirstOrDefault(u => u.Correo == "admin@test.com");

        if (adminUser != null)
            Console.WriteLine($"Admin user found: {adminUser.Nombre}");
        else
            Console.WriteLine("WARNING: Admin user not found.");
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
            Console.WriteLine("Created uploads directory: " + uploadsFolder);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"UPLOAD DIR ERROR: {ex.Message}");
    }
}



app.Run();
