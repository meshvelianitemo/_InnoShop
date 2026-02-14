using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProductManagement.Controllers;
using ProductManagement.Models.Data;
using ProductManagement.Models.DTOs;
using ProductManagement.Services;
using System.Security.Claims;
using Xunit;

namespace ProductManagement.Tests.Controllers
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductService> _mockService;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _mockService = new Mock<IProductService>();
            _controller = new ProductsController(_mockService.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        private void SetUser(int userId)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Client")
        };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext.User =
                new ClaimsPrincipal(identity);
        }


        [Fact]
        public async Task Products_ReturnsOk()
        {
            var pagination = new PaginationParameters { Page = 1, PageSize = 10 };

            var result = new PagedResult<ProductResponseDto>
            {
                Items = new List<ProductResponseDto>(),
                TotalCount = 0
            };

            _mockService
                .Setup(s => s.GetAllAsync(It.IsAny<ProductQueryParameters>()))
                .ReturnsAsync(result);

            var response = await _controller.Products(pagination);

            var ok = Assert.IsType<OkObjectResult>(response);
            Assert.Equal(result, ok.Value);
        }

        [Fact]
        public async Task GetProductByName_ReturnsOk()
        {
            var pagination = new PaginationParameters { Page = 1, PageSize = 5 };

            _mockService
                .Setup(s => s.GetAllAsync(It.IsAny<ProductQueryParameters>()))
                .ReturnsAsync(new PagedResult<ProductResponseDto>());

            var result = await _controller.GetProductByName("phone", pagination);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetForUser_ReturnsUserProducts()
        {
            SetUser(5);

            _mockService
                .Setup(s => s.GetAllForUserAsync(5, It.IsAny<ProductQueryParameters>()))
                .ReturnsAsync(new PagedResult<ProductResponseDto>());

            var result = await _controller.GetForUser(new ProductQueryParameters());

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetForUser_Throws_WhenUserIdMissing()
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _controller.GetForUser(new ProductQueryParameters())
            );
        }


        [Fact]
        public async Task GetById_ReturnsProduct()
        {
            var dto = new ProductResponseDto { ProductId = 1, Name = "Test" };

            _mockService
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(dto);

            var result = await _controller.GetById(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, ok.Value);
        }


        [Fact]
        public async Task Update_ReturnsNoContent()
        {
            SetUser(3);

            _mockService
                .Setup(s => s.UpdateAsync(1, It.IsAny<UpdateProductDTO>(), 3))
                .ReturnsAsync(true);

            var result = await _controller.Update(1, new UpdateProductDTO());

            Assert.IsType<NoContentResult>(result);
        }


        [Fact]
        public async Task Create_ReturnsCreatedProduct()
        {
            SetUser(7);

            var dto = new CreateProductDTO { Name = "New Product" };

            _mockService
                .Setup(s => s.CreateAsync(dto, 7))
                .ReturnsAsync(new Product());

            var result = await _controller.Create(dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AddImage_ReturnsBadRequest_WhenNoImages()
        {
            SetUser(1);

            var result = await _controller.AddImage(1, new List<IFormFile>());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddImage_ReturnsNoContent_WhenValid()
        {
            SetUser(1);

            var images = new List<IFormFile>
    {
        new FormFile(Stream.Null, 0, 1, "img", "img.jpg")
    };

            _mockService
                .Setup(s => s.AddImageAsync(1, images, 1))
                .Returns(Task.CompletedTask);

            var result = await _controller.AddImage(1, images);

            Assert.IsType<NoContentResult>(result);
        }


        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            SetUser(4);

            _mockService
                .Setup(s => s.DeleteAsync(1, 4))
                .ReturnsAsync(true);

            var result = await _controller.Delete(1);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task RemoveImage_ReturnsNoContent()
        {
            SetUser(2);

            _mockService
                .Setup(s => s.RemoveImageAsync(5, 2, 1))
                .Returns(Task.CompletedTask);

            var result = await _controller.RemoveImage(5, 1);

            Assert.IsType<NoContentResult>(result);
        }



    }
}
