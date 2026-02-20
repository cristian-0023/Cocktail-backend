using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;

        public override int SaveChanges()
        {
            EnforceUtcOnTrackedEntities();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            EnforceUtcOnTrackedEntities();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void EnforceUtcOnTrackedEntities()
        {
            var entries = ChangeTracker.Entries();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    foreach (var property in entry.Properties)
                    {
                        var clrType = property.Metadata.ClrType;

                        // Manejo de DateTime (No permitir Kind=Local)
                        if (clrType == typeof(DateTime) || clrType == typeof(DateTime?))
                        {
                            if (property.CurrentValue != null)
                            {
                                DateTime dt = (DateTime)property.CurrentValue;
                                if (dt.Kind != DateTimeKind.Utc)
                                {
                                    property.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. UTC Logic for Npgsql (Modern approach for 8.0+)
            // Usamos tipos nativos "timestamp with time zone"
            // Npgsql 6.0+ mapea automáticamente estos tipos a DateTime con Kind=Utc al leer.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetColumnType("timestamp with time zone");
                    }
                }
            }

            // 2. Decimal Precision
            modelBuilder.Entity<Product>().Property(p => p.Precio).HasPrecision(10, 2);
            modelBuilder.Entity<CartItem>().Property(i => i.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<OrderItem>().Property(i => i.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Invoice>().Property(i => i.TotalAmount).HasPrecision(18, 2);

            // 3. Relationships & Keys (Defense in Depth)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Rol)
                .WithMany()
                .HasForeignKey(u => u.IdRol)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.IdProducto);

            // 4. Seeding Data
            // Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { IdRol = 1, NombreRol = "Admin" },
                new Role { IdRol = 2, NombreRol = "Invitado" }
            );

            // Products (15 Items)
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

            // Admin User (Stable Hash to avoid migration flapping)
            // Password: "Cristian@019"
            string stableHash = "$2a$11$Xm77ZfGz/4r.Y1D2oB7Q/ueo.D0Gz.yJ6W.Vv2o.P6iMhO7L6J9XyG"; 
            
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    IdUsuario = 1,
                    Nombre = "Cristian",
                    Correo = "admin@test.com",
                    Contrasena = stableHash,
                    IdRol = 1,
                    Estado = true,
                    ImagenPerfilURL = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTFKR2kN9cKDOCxsv7O6Mnt6teFUQgwMLV-eQ&s"
                }
            );
        }
    }
}
