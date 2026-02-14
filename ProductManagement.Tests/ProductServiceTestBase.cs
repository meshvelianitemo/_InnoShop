using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProductManagement.Models.Data;
using ProductManagement.Services;

namespace ProductManagement.Tests
{
    public abstract class ProductServiceTestBase
    {
        protected ProductDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ProductDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ProductDbContext(options);
        }

        protected Mock<ILogger<ProductService>> CreateLogger()
            => new();

        protected Mock<IWebHostEnvironment> CreateEnv(string rootPath)
        {
            var mock = new Mock<IWebHostEnvironment>();
            mock.Setup(e => e.ContentRootPath).Returns(rootPath);
            return mock;
        }
    }
}
