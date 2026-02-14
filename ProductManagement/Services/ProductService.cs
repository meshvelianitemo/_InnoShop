using Microsoft.EntityFrameworkCore;
using NPOI.OpenXmlFormats.Dml.Chart;
using ProductManagement.Exceptions;
using ProductManagement.Models.Data;
using ProductManagement.Models.DTOs;


namespace ProductManagement.Services
{
    public class ProductService : IProductService
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<ProductService> _logger;
        private readonly IWebHostEnvironment _env;
        public ProductService(ProductDbContext context, ILogger<ProductService> logger, IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        public async Task AddImageAsync(int productId, List<IFormFile> images, int userId)
        {
            _logger.LogInformation(
                "AddImageAsync started. ProductId={ProductId}, UserId={UserId}, Files={Count}",
                productId,
                userId,
                images.Count
            );

            var existingProduct = await _context.Products
                        .FirstOrDefaultAsync(p => p.ProductId == productId && p.UserId == userId);

            if (existingProduct == null)
            {
                _logger.LogWarning(
                    "Product not found or access denied. ProductId={ProductId}, UserId={UserId}",
                    productId,
                    userId
                     );
                throw new ProductNotFoundException("The product was not found!");
            }

            //../uploads/products/

            string uploadsRoot;

            try
            {
                uploadsRoot = Path.GetFullPath(
                    Path.Combine(_env.ContentRootPath, "..", "Uploads", "Products")
                );

                _logger.LogInformation("Resolved uploads path: {UploadsRoot}", uploadsRoot);

                if (!Directory.Exists(uploadsRoot))
                {
                    Directory.CreateDirectory(uploadsRoot);
                    _logger.LogInformation("Uploads directory created");
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed resolving or creating upload directory");
                throw;
            }

            foreach (var file in images)
            {
                _logger.LogInformation(
                    "Processing file {FileName}, Size={Size}",
                    file.FileName,
                    file.Length
                );

                if (file.Length == 0)
                {
                    _logger.LogWarning("Skipping empty file: {FileName}", file.FileName);
                    continue;
                }

                try
                {
                    var extension = Path.GetExtension(file.FileName);
                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var fullPath = Path.Combine(uploadsRoot, fileName);

                    _logger.LogInformation("Saving file to {FullPath}", fullPath);

                    await using var stream = new FileStream(fullPath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = productId,
                        ImagePath = Path.Combine("Uploads", "Products", fileName)
                            .Replace("\\", "/")
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed while processing file {FileName}",
                        file.FileName
                    );
                    throw;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Images saved successfully for ProductId={ProductId}", productId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while saving product images");
                throw new DatabaseOperationException("Failed to save product images.");
            }

        }


        public async Task<Product> CreateAsync(CreateProductDTO dto, int userId)
        {
            _logger.LogInformation("CreateAsync called. UserId={UserId}, ProductName={ProductName}, Category={CategoryName}",
                userId, dto.Name, dto.CategoryName);

            // Check category
            var existingCategory = await _context.ProductCategories
                .FirstOrDefaultAsync(c => c.CategoryName == dto.CategoryName);

            if (existingCategory == null)
            {
                _logger.LogWarning("Category not found: {CategoryName}", dto.CategoryName);
                throw new CategoryNotFoundException($"Category '{dto.CategoryName}' was not found");
            }

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Amount = dto.Amount,
                UserId = userId,
                CategoryId = existingCategory.CategoryId
            };

            _context.Products.Add(product);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Product successfully saved. ProductId={ProductId}", product.ProductId);
                return product;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DB error while creating product. ProductName={ProductName}", dto.Name);
                throw new DatabaseOperationException("Failed to save the product.");
            }
        }


        public async Task<bool> DeleteAsync(int productId, int userId)
        {
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId && p.UserId == userId);

            if (existingProduct == null)
            {
                _logger.LogError($"The product was not found!");
                throw new ProductNotFoundException();
            }

            _context.Remove(existingProduct);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("The Product was successfully removed!");
                return true;
            }
            catch (DbUpdateException)
            {
                throw new DatabaseOperationException("Failed to delete the Product!");
            }
        }

        public async Task<PagedResult<ProductResponseDto>> GetAllAsync(ProductQueryParameters query)
        {
            var page = query.Page ?? 1;
            var pageSize = query.PageSize ?? 20;

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 20 : pageSize;


            var baseQuery = _context.Products.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(query.Search) &&
                !query.Search.Equals("string", StringComparison.OrdinalIgnoreCase))
            {
                var search = query.Search.Trim();
                baseQuery = baseQuery.Where(p =>
                    EF.Functions.Like(p.Name, $"%{search}%"));
            }


            if (query.CategoryId.HasValue && query.CategoryId >0)
                baseQuery = baseQuery.Where(p => p.CategoryId == query.CategoryId);

            if (query.MinPrice.HasValue)
                baseQuery = baseQuery.Where(p => p.Price >= query.MinPrice);

            if (query.MaxPrice.HasValue)
                baseQuery = baseQuery.Where(p => p.Price <= query.MaxPrice);

            if (query.Available == true)
                baseQuery = baseQuery.Where(p => p.Amount > 0);
            else if (query.Available == false)
                baseQuery = baseQuery.Where(p => p.Amount == 0);


            // Get total count before pagination
            var totalCount = await baseQuery.CountAsync();

            // Apply pagination
            
            var products = await baseQuery
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.CreationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = products.Select(p => new ProductResponseDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Amount = p.Amount,
                ImagePaths = p.ProductImages.Any()
                    ? p.ProductImages.Select(i => i.ImagePath).ToList()
                    : null,
                UserId = p.UserId,
                CategoryId = p.CategoryId,
                CreationDate = p.CreationDate,
                ModifiedDate = p.ModifiedDate
            }).ToList();

            return new PagedResult<ProductResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };


        }

        public async Task<PagedResult<ProductResponseDto>> GetAllForUserAsync(int userId, ProductQueryParameters query)
        {
            var page = query.Page ?? 1;
            var pageSize = query.PageSize ?? 20;

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 20 : pageSize;

            var products = _context.Products .
                Where(p => p.UserId == userId);

            if (products == null)
            {
                _logger.LogError("No products found for the user with id : " + userId);
                throw new ProductNotFoundException("No products found!");
            }

            // counting before Pagination
            var totalCount = await products.CountAsync();

            // Apply pagination
            var paginatedProducts = await products
            .Include(p => p.ProductImages)
            .OrderByDescending(p => p.CreationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

            var items = paginatedProducts.Select(p => new ProductResponseDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Amount = p.Amount,
                ImagePaths = p.ProductImages.Any()
        ? p.ProductImages.Select(i => i.ImagePath).ToList()
        : null,
                UserId = p.UserId,
                CategoryId = p.CategoryId,
                CreationDate = p.CreationDate,
                ModifiedDate = p.ModifiedDate
            }).ToList();


            return new PagedResult<ProductResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<ProductResponseDto?> GetByIdAsync(int productId)
        {
            var product = await _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                throw new ProductNotFoundException();

            return new ProductResponseDto
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Amount = product.Amount,
                ImagePaths = product.ProductImages.Any()
                    ? product.ProductImages.Select(i => i.ImagePath).ToList()
                    : null,
                UserId = product.UserId,
                CategoryId = product.CategoryId,
                CreationDate = product.CreationDate,
                ModifiedDate = product.ModifiedDate
            };
        

        }

        public async Task RemoveImageAsync(int imageId, int userId, int productId)
        {
            var ownerUserId = await _context.Products
                .Where(p => p.ProductId == productId)
                .Select(p => p.UserId)
                .FirstOrDefaultAsync();

            var image = await _context.ProductImages
                .FirstOrDefaultAsync(p => p.Id == imageId && ownerUserId ==userId);

            if (image == null)
            {
                throw new ImageNotFoundException();
            }
            var absolutePath = Path.Combine(_env.ContentRootPath, "..", image.ImagePath);
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);

            _context.ProductImages.Remove(image);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("the Image was successfully removed!");
            }
            catch (DbUpdateException)
            {
                throw new DatabaseOperationException("Failed to delete the image!");
            }
        }

        public async Task<bool> UpdateAsync(int productId, UpdateProductDTO dto, int userId)
        {
            var product = await _context.Products.
                    FirstOrDefaultAsync(p => p.ProductId == productId && p.UserId == userId);

            if (product == null)
            {
                _logger.LogError($"The product was not found or does not belong to the User!");
                throw new ProductNotFoundException();
            }

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Amount = dto.Amount;
            product.ModifiedDate = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("The Product was successfully updated!");
                return true;
            }
            catch (DbUpdateException)
            {
                throw new DatabaseOperationException("Failed to update the Product!");
            }
        }
    }
}
