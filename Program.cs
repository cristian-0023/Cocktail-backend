using Cocktail.back.Data;
using Cocktail.back.Repositories;
using Cocktail.back.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// CORS
builder.Services.AddCors(options =>
{
                // Configurar política específica para el frontend
                options.AddPolicy("AllowFrontend",
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:5173")
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials(); // Permitir credenciales (cookies/headers de autenticación)
                    });
});

// JWT Authentication
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

var app = builder.Build();

// Auto-create database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try 
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        Console.WriteLine("Initializing database schema (v2)...");
        context.Database.EnsureCreated();
        Console.WriteLine("Database initialized successfully.");
        
        // Verify admin user exists
        var adminUser = context.Users.FirstOrDefault(u => u.Correo == "admin@test.com");
        if (adminUser != null)
        {
            Console.WriteLine($"✅ Admin user found: {adminUser.Nombre} (ID: {adminUser.IdUsuario})");
            Console.WriteLine($"   Email: {adminUser.Correo}");
            Console.WriteLine($"   Role: {adminUser.Rol?.NombreRol ?? "NULL"}");

            // Validar y actualizar imagen si es necesario (Fix usuario)
            string newImageUrl = @"https://chatgpt.com/s/m_698f9c808e648191b46acfd0c376c8bd";
            if (adminUser.ImagenPerfilURL != newImageUrl)
            {
                Console.WriteLine("Updating admin profile image...");
                adminUser.ImagenPerfilURL = newImageUrl;
                context.SaveChanges();
                Console.WriteLine("Admin profile image updated.");
            }
        }
        else
        {
            Console.WriteLine("⚠️ WARNING: Admin user NOT found in database!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DATABASE ERROR: {ex.Message}");
    }
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[GLOBAL ERROR] {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message, detail = "Ver logs del servidor." });
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseStaticFiles();



app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
