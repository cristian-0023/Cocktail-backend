using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cocktail.back.DTOs;
using Cocktail.back.Models;
using Cocktail.back.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cocktail.back.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Cocktail.back.DTOs.ProductDto productDto)
        {
            var product = await _productService.CreateProductAsync(productDto);
            return CreatedAtAction(nameof(GetById), new { id = product.IdProducto }, product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto productDto)
        {
            var product = await _productService.UpdateProductAsync(id, productDto);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _productService.DeleteProductAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se ha seleccionado ningún archivo.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return BadRequest("Formato de imagen no permitido (solo jpg, png, webp).");

            if (file.Length > 2 * 1024 * 1024) // 2MB limit
                return BadRequest("El archivo es demasiado grande (máximo 2MB).");

            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var imagePath = $"/uploads/products/{fileName}";
            return Ok(new { imagePath });
        }
    }
}
