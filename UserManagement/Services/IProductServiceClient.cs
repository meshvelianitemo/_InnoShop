using Microsoft.AspNetCore.Mvc;
using ProductManagement.Models.DTOs;
using UserManagement.Models.DTOs;

namespace UserManagement.Services
{
    public interface IProductServiceClient
    {
        // GET api/products/{id}
        Task<ProductDTO?> GetProductByIdAsync(int productId, string? jwtToken = null);
        // GET api/products/Create
        Task<ProductDTO> CreateAsync(CreateProductDTO dto, string jwt);

        // GET api/products/All
        Task<ProductListDTO> GetAllAsync(string? jwtToken = null);

        // GET api/products/Title?name=...
        Task<ProductListDTO> GetByNameAsync(string name, string? jwtToken = null);

        // GET api/products/Mine
        Task<ProductListDTO> GetForUserAsync(string? jwtToken = null);

        // GET api/products/Filter?search=...&categoryId=...&minPrice=...&maxPrice=...
        Task<ProductListDTO> GetFilteredProducts(ProductFilterParameters dto);

        // PUT api/products/{id}
        Task UpdateAsync(int productId, UpdateProductDTO dto, string? jwtToken = null);

        // DELETE api/products/{id}
        Task DeleteAsync(int productId, string? jwtToken = null);

        // POST api/products/{id}/images
        Task AddImageAsync(int productId, List<IFormFile> images, string? jwtToken = null);

        // DELETE api/products/images/{imageId}
        Task RemoveImageAsync(int imageId,int productId, string? jwtToken = null);
    }
}
