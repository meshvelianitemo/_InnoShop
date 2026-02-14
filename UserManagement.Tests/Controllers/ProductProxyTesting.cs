using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ProductManagement.Models.DTOs;
using UserManagement.Controllers;
using UserManagement.Models.DTOs;
using UserManagement.Services;
using Xunit;

namespace UserManagement.Tests.Controllers
{
    public class ProductProxyControllerTests
    {
        private readonly Mock<IProductServiceClient> _mockProductServiceClient;
        private readonly Mock<ILogger<ProductProxyController>> _mockLogger;
        private readonly ProductProxyController _controller;

        public ProductProxyControllerTests()
        {
            _mockProductServiceClient = new Mock<IProductServiceClient>();
            _mockLogger = new Mock<ILogger<ProductProxyController>>();

            _controller = new ProductProxyController(
                _mockProductServiceClient.Object,
                _mockLogger.Object
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region GetProductById Tests
        [Fact]
        public async Task GetProductById_ReturnsOk_WhenProductExists()
        {
            var productId = 1;
            var dto = new ProductDTO { ProductId = productId, Name = "Test Product" };

            _mockProductServiceClient
                .Setup(s => s.GetProductByIdAsync(productId, null))
                .ReturnsAsync(dto);

            var result = await _controller.GetProductById(productId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode ?? 200);
            Assert.Equal(dto, okResult.Value);
        }

        [Fact]
        public async Task GetProductById_ReturnsNotFound_WhenProductDoesNotExist()
        {
            var productId = 99;
            _mockProductServiceClient
                .Setup(s => s.GetProductByIdAsync(productId, null))
                .ReturnsAsync((ProductDTO?)null);

            var result = await _controller.GetProductById(productId);

            Assert.IsType<NotFoundResult>(result);
        }
        #endregion

        #region GetAllProducts Tests
        [Fact]
        public async Task GetAllProducts_ReturnsOk_WithProducts()
        {
            var wrapper = new ProductListDTO
            {
                Items = new List<ProductDTO>
                {
                    new() { ProductId = 1, Name = "P1" },
                    new() { ProductId = 2, Name = "P2" }
                },
                TotalCount = 2
            };

            _mockProductServiceClient
                .Setup(s => s.GetAllAsync(null))
                .ReturnsAsync(wrapper);

            var result = await _controller.GetAllProducts();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<ProductListDTO>(okResult.Value);
            Assert.Equal(2, returned.Items.Count());
        }
        #endregion

        #region GetByName Tests
        [Fact]
        public async Task GetByName_ReturnsOk_WithProducts()
        {
            var search = "Test";
            var wrapper = new ProductListDTO
            {
                Items = new List<ProductDTO>
                {
                    new() { ProductId = 1, Name = "Test Product" }
                },
                TotalCount = 1
            };

            _mockProductServiceClient
                .Setup(s => s.GetByNameAsync(search, null))
                .ReturnsAsync(wrapper);

            var result = await _controller.GetByName(search);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<ProductListDTO>(okResult.Value);
            Assert.Single(returned.Items);
        }
        #endregion

        #region Create Tests
        [Fact]
        public async Task Create_ReturnsOk_WhenJwtPresent()
        {
            var jwt = "fake-jwt";
            var dto = new CreateProductDTO { Name = "New Product" };

            _controller.ControllerContext.HttpContext = new DefaultHttpContext();
            _controller.ControllerContext.HttpContext.Request.Headers["Cookie"] = $"JwtToken={jwt}";
            

            _mockProductServiceClient
                .Setup(s => s.CreateAsync(dto, jwt))
                .ReturnsAsync(new ProductDTO { ProductId = 1, Name = dto.Name });

            var result = await _controller.Create(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode ?? 200);
        }

        [Fact]
        public async Task Create_ReturnsUnauthorized_WhenJwtMissing()
        {
            var dto = new CreateProductDTO { Name = "New Product" };

            var result = await _controller.Create(dto);

            Assert.IsType<UnauthorizedResult>(result);
        }
        #endregion

        #region UpdateProduct Tests
        [Fact]
        public async Task UpdateProduct_ReturnsNoContent_WhenSuccessful()
        {
            var jwt = "jwt";
            var productId = 1;
            var dto = new UpdateProductDTO { Name = "Updated" };
            _controller.ControllerContext.HttpContext = new DefaultHttpContext();
            _controller.ControllerContext.HttpContext.Request.Headers["Cookie"] = $"JwtToken={jwt}";

            _mockProductServiceClient
                .Setup(s => s.UpdateAsync(productId, dto, jwt))
                .Returns(Task.CompletedTask);

            var result = await _controller.UpdateProduct(productId, dto);

            Assert.IsType<NoContentResult>(result);
        }
        #endregion

        #region DeleteProduct Tests
        [Fact]
        public async Task DeleteProduct_ReturnsNoContent_WhenSuccessful()
        {
            var jwt = "jwt";
            var productId = 1;
            _controller.ControllerContext.HttpContext = new DefaultHttpContext();
            _controller.ControllerContext.HttpContext.Request.Headers["Cookie"] = $"JwtToken={jwt}";

            _mockProductServiceClient
                .Setup(s => s.DeleteAsync(productId, jwt))
                .Returns(Task.CompletedTask);

            var result = await _controller.DeleteProduct(productId);

            Assert.IsType<NoContentResult>(result);
        }
        #endregion
    }
}
