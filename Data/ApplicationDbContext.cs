using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Cocktail.back.Models;
using BCrypt.Net;

namespace Cocktail.back.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Invoice> Invoices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ================= CONFIGURACIÓN GLOBAL UTC (BLINDAJE TOTAL) =================
            // Fuerza a que todos los DateTime se guarden como UTC y se lean como UTC
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(
                            new ValueConverter<DateTime, DateTime>(
                                v => v.ToUniversalTime(),
                                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                            )
                        );
                        // Asegurar que el tipo en base de datos sea 'timestamp with time zone' (timestamptz)
                        property.SetColumnType("timestamp with time zone");
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(
                            new ValueConverter<DateTime?, DateTime?>(
                                v => !v.HasValue ? v : v.Value.ToUniversalTime(),
                                v => !v.HasValue ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                            )
                        );
                        // Asegurar que el tipo en base de datos sea 'timestamp with time zone' (timestamptz)
                        property.SetColumnType("timestamp with time zone");
                    }
                }
            }

            // ================= CONFIGURACIÓN DECIMAL =================
            modelBuilder.Entity<Product>()
                .Property(p => p.Precio)
                .HasPrecision(10, 2); // 10 dígitos total, 2 decimales

            // ================= SEED ROLES =================
            modelBuilder.Entity<Role>().HasData(
                new Role { IdRol = 1, NombreRol = "Admin" },
                new Role { IdRol = 2, NombreRol = "Invitado" }
            );

            // ================= SEED PRODUCTS =================
            modelBuilder.Entity<Product>().HasData(
                new Product { IdProducto = 1, Nombre = "Granizado de Fresa", Description = "Clásico refrescante de fresas silvestres.", Precio = 12000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ17o_k7g2VYrkVZ_4y8CStSZvMOi45NXFT4A&s" },
                new Product { IdProducto = 2, Nombre = "Granizado de Limón", Description = "El balance perfecto entre ácido y dulce.", Precio = 10000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSlRLGzL5MU25UJ0N3OSkTRgU2FYs9II_OPvA&s" },
                new Product { IdProducto = 3, Nombre = "Granizado Maracuyá", Description = "Exótica mezcla de fruta de la pasión.", Precio = 15000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTjhWMiciB7c7-idN0aI2gG_54Jaj5lZ9_M3g&s" },
                new Product { IdProducto = 4, Nombre = "Granizado de Café", Description = "Café premium helado con un toque de crema.", Precio = 18000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQK5CQAxTJrYB8OQ9Z_6dER3lDWQkiPVQCrZA&s" },
                new Product { IdProducto = 5, Nombre = "Granizado de Mango", Description = "Mango maduro seleccionado para tu paladar.", Precio = 14000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTCLJI-GmPoCPjCuQVrDv3t667Zbg9YG1DoEA&s" },
                new Product { IdProducto = 6, Nombre = "Granizado Cereza", Description = "Deliciosa cereza roja cristalizada.", Precio = 13000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQEu6kRhxyoxAui2-9j6BEAvAxd-MbgfyZs4w&s" },
                new Product { IdProducto = 7, Nombre = "Granizado Mandarina", Description = "Frescura cítrica natural naranja.", Precio = 12000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSFK_AHSyYjU-oxUY3OsCvXTT6okyTimSu1xw&s" },
                new Product { IdProducto = 8, Nombre = "Granizado de Coco", Description = "Suave crema de coco tropical blanca.", Precio = 16000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR3H3cvkoyfX0oDBsTnREyBy767x_XAGSdCiw&s" },
                new Product { IdProducto = 9, Nombre = "Granizado Blue Hawaii", Description = "Sabor a cielo azul y diversión.", Precio = 17000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSZp_E3lcr82xQKw1_xiD-KKrhmceuSqiivUA&s" },
                new Product { IdProducto = 10, Nombre = "Granizado de Mora", Description = "Extracto de mora 100% natural púrpura.", Precio = 11000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ6kerJDuiPpUDTxJFUtakxsurGp1t13EwxTg&s" },
                new Product { IdProducto = 11, Nombre = "Granizado Piña", Description = "Piña oro miel en punto frío amarilla.", Precio = 13000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTNmtvuWLaxR39WdLgZ5ZiTMEBsQQYzpst8RA&s" },
                new Product { IdProducto = 12, Nombre = "Granizado Sandia", Description = "Hidratación y dulzura de sandía roja.", Precio = 10000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSdqe_7VpHcPPrTyvjHI2pr2QTEDN_4sdkrdw&s" },
                new Product { IdProducto = 13, Nombre = "Granizado Green Apple", Description = "Manzana verde ácida y fría.", Precio = 11000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcReAFVo1ezRmQbzlYYiAEGanaAW1SduLjbGXA&s" },
                new Product { IdProducto = 14, Nombre = "Granizado Tropical", Description = "Mezcla de todas las frutas del caribe.", Precio = 18000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcT7PQDJpJGYl6__M5UhsHGjqgv0dlpR96HOGg&s" },
                new Product { IdProducto = 15, Nombre = "Granizado Rainbow", Description = "Toque de mil colores y saludable.", Precio = 15000, ImagenURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSrSdyXSVmTVI9ILfCa1bW5V7CgLOU-im-wOA&s" }
            );

            // ================= ADMIN DEFAULT =================
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    IdUsuario = 1,
                    Nombre = "Cristian",
                    Correo = "admin@test.com",
                    Contrasena = BCrypt.Net.BCrypt.HashPassword("Cristian@019"),
                    IdRol = 1,
                    Estado = true,
                    ImagenPerfilURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTFKR2kN9cKDOCxsv7O6Mnt6teFUQgwMLV-eQ&s"
                }
            );
        }
    }
}
