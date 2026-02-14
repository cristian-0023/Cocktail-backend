using System.Threading.Tasks;
using System.Collections.Generic;
using Cocktail.back.DTOs;
using Cocktail.back.Models;
using Microsoft.EntityFrameworkCore;
using Cocktail.back.Data; // Keep this as ApplicationDbContext is used

namespace Cocktail.back.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product> CreateAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product?> UpdateAsync(Product product)
        {
             var existingProduct = await _context.Products.FindAsync(product.IdProducto);
             if (existingProduct == null) return null;

             existingProduct.Nombre = product.Nombre;
             existingProduct.Description = product.Description;
             existingProduct.Precio = product.Precio;
             existingProduct.ImagenURL = product.ImagenURL;
             existingProduct.Disponibilidad = product.Disponibilidad;

             await _context.SaveChangesAsync();
             return existingProduct;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
