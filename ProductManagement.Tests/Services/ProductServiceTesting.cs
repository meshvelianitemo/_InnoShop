using ProductManagement.Exceptions;
using ProductManagement.Models.Data;
using ProductManagement.Models.DTOs;
using ProductManagement.Services;

namespace ProductManagement.Tests.Services
{
    public class ProductServiceTesting : ProductServiceTestBase
    {
        [Fact]
        public async Task CreateAsync_CreatesProduct_WhenCategoryExists()
        {
            var context = CreateDbContext();
            context.ProductCategories.Add(new ProductCategory
            {
                CategoryId = 1,
                CategoryName = "Electronics"
            });
            await context.SaveChangesAsync();

            var service = new ProductService(
                context,
                CreateLogger().Object,
                CreateEnv(Path.GetTempPath()).Object
            );

            var dto = new CreateProductDTO
            {
                Name = "Laptop",
                Description = "Test",
                Price = 1000,
                Amount = 5,
                CategoryName = "Electronics"
            };

            var product = await service.CreateAsync(dto, userId: 1);

            Assert.NotNull(product);
            Assert.Equal("Laptop", product.Name);
            Assert.Equal(1, product.UserId);
        }


        [Fact]
        public async Task CreateAsync_Throws_WhenCategoryMissing()
        {
            var context = CreateDbContext();
            var service = new ProductService(
                context,
                CreateLogger().Object,
                CreateEnv(Path.GetTempPath()).Object
            );

            var dto = new CreateProductDTO
            {
                Name = "Laptop",
                CategoryName = "Invalid"
            };

            await Assert.ThrowsAsync<CategoryNotFoundException>(
                () => service.CreateAsync(dto, 1)
            );
        }


        [Fact]
        public async Task GetByIdAsync_ReturnsProduct_WhenExists()
        {
            var context = CreateDbContext();
            context.Products.Add(new Product
            {
                ProductId = 1,
                Name = "Phone",
                UserId = 1
            });
            await context.SaveChangesAsync();

            var service = new ProductService(
                context,
                CreateLogger().Object,
                CreateEnv(Path.GetTempPath()).Object
            );

            var result = await service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Phone", result.Name);
        }


        [Fact]
        public async Task GetByIdAsync_Throws_WhenNotFound()
        {
            var service = new ProductService(
                CreateDbContext(),
                CreateLogger().Object,
                CreateEnv(Path.GetTempPath()).Object
            );

            await Assert.ThrowsAsync<ProductNotFoundException>(
                () => service.GetByIdAsync(99)
            );
        }

        [Fact]
        public async Task UpdateAsync_UpdatesProduct_WhenOwnedByUser()
        {
            var context = CreateDbContext();
            context.Products.Add(new Product
            {
                ProductId = 1,
                Name = "Old",
                UserId = 1
            });
            await context.SaveChangesAsync();

            var service = new ProductService(
                context,
                CreateLogger().Object,
                CreateEnv(Path.GetTempPath()).Object
            );

            var dto = new UpdateProductDTO
            {
                Name = "New",
                Price = 200,
                Amount = 10
            };

            var result = await service.UpdateAsync(1, dto, 1);

            Assert.True(result);
            Assert.Equal("New", context.Products.First().Name);
        }


        [Fact]
        public async Task DeleteAsync_RemovesProduct_WhenOwned()
        {
            var context = CreateDbContext();
            context.Products.Add(new Product
            {
                ProductId = 1,
                UserId = 1
            });
            await context.SaveChangesAsync();

            var service = new ProductService(
                context,
                CreateLogger().Object,
                CreateEnv(Path.GetTempPath()).Object
            );

            var result = await service.DeleteAsync(1, 1);

            Assert.True(result);
            Assert.Empty(context.Products);
        }



    }
}