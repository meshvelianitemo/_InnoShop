using ProductManagement.Models.DTOs;
using ProductManagement.Models.Data;


namespace ProductManagement.Services
{
    public interface IProductService
    {
        Task<Product> CreateAsync(CreateProductDTO dto, int userId);
        Task<ProductResponseDto?> GetByIdAsync(int productId);
        Task<PagedResult<ProductResponseDto>> GetAllAsync(ProductQueryParameters query);
        Task<bool> UpdateAsync(int productId, UpdateProductDTO dto, int userId);
        Task<bool> DeleteAsync(int productId, int userId);
        Task AddImageAsync(int productId, List<IFormFile> imagePath, int userId);
        Task RemoveImageAsync(int imageId, int userId, int productId);
        Task<PagedResult<ProductResponseDto>> GetAllForUserAsync(int userId, ProductQueryParameters query);



    }
}
