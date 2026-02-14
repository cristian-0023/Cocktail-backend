using System.Threading.Tasks;
using System.Collections.Generic;
using Cocktail.back.DTOs;
using Cocktail.back.Models;
using Cocktail.back.Repositories;

namespace Cocktail.back.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;

        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<Product> CreateProductAsync(ProductDto productDto)
        {
            var product = new Product
            {
                Nombre = productDto.Nombre,
                Description = productDto.Description,
                Precio = productDto.Precio,
                ImagenURL = productDto.ImagenURL,
                Disponibilidad = productDto.Disponibilidad
            };

            return await _repository.CreateAsync(product);
        }

        public async Task<Product?> UpdateProductAsync(int id, ProductDto productDto)
        {
              var productToUpdate = new Product
              {
                  IdProducto = id,
                  Nombre = productDto.Nombre,
                  Description = productDto.Description,
                  Precio = productDto.Precio,
                  ImagenURL = productDto.ImagenURL,
                  Disponibilidad = productDto.Disponibilidad
              };

             return await _repository.UpdateAsync(productToUpdate);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
