using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using UserManagement.Exceptions;
using UserManagement.Models.DTOs;
using UserManagement.Services;

namespace UserManagement.Tests.Services
{
    public class ProductServiceClientTests
    {
        private readonly Mock<ILogger<ProductServiceClient>> _mockLogger;
        private readonly Mock<IUserService> _mockUserService;
        private const string BaseUrl = "https://fake-api.com/";
        public ProductServiceClientTests()
        {
            _mockLogger = new Mock<ILogger<ProductServiceClient>>();
            _mockUserService = new Mock<IUserService>();
        }

        private ProductServiceClient CreateClient(HttpResponseMessage response)
        {
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(response);

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(BaseUrl)
            };

            return new ProductServiceClient(httpClient,_mockLogger.Object,_mockUserService.Object);
        }


        [Fact]
        public async Task GetProductByIdAsync_ReturnsProduct_WhenSuccess()
        {
            // Arrange
            var product = new ProductDTO
            {
                ProductId = 1,
                Name = "Test Product",
                Description = "A product for testing",
                Price = 100,
                Amount = 10,
                UserId = 1,
                CategoryId = 1,
                CreationDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                ImagePaths = new List<string> { "image1.jpg", "image2.jpg" }
            };

            var json = JsonSerializer.Serialize(product);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var client = CreateClient(response);

            // Act
            var result = await client.GetProductByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ProductId);
            Assert.Equal("Test Product", result.Name);
        }

        [Fact]
        public async Task GetProductByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.NotFound);

            var client = CreateClient(response);

            // Act
            var result = await client.GetProductByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProductByIdAsync_ThrowsException_WhenServerError()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Server crashed")
            };

            var client = CreateClient(response);

            // Act & Assert
            await Assert.ThrowsAsync<CustomException.ProductServiceResponseException>(
                () => client.GetProductByIdAsync(1)
            );
        }


        [Fact]
        public async Task GetProductByIdAsync_ThrowsDeserializationException_WhenInvalidJson()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not valid json")
            };

            var client = CreateClient(response);

            // Act & Assert
            await Assert.ThrowsAsync<CustomException.ProductServiceDeserializationException>(
                () => client.GetProductByIdAsync(1)
            );
        }


        [Fact]
        public async Task GetProductByIdAsync_ThrowsConnectionException_WhenHttpFails()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                   "SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>()
               )
               .ThrowsAsync(new HttpRequestException("Network error"));

            var httpClient = new HttpClient(handlerMock.Object);
            var client = new ProductServiceClient(httpClient, _mockLogger.Object, _mockUserService.Object);

            // Act & Assert
            await Assert.ThrowsAsync<CustomException.ProductServiceConnectionException>(
                () => client.GetProductByIdAsync(1)
            );
        }


    }
}
