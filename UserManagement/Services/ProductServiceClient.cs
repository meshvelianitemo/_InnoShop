using Microsoft.AspNetCore.Mvc;
using ProductManagement.Models.DTOs;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using UserManagement.Exceptions;
using UserManagement.Models.DTOs;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace UserManagement.Services
{
    public class ProductServiceClient : IProductServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductServiceClient> _logger;
        private readonly IUserService _userService;
        public ProductServiceClient(HttpClient httpClient, ILogger<ProductServiceClient> logger, IUserService userService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _userService = userService;
        }

        //helper method to add JWT token to request headers
        private void AddJwt(HttpRequestMessage request, string? jwtToken)
        {
            if (!string.IsNullOrEmpty(jwtToken))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            }
        }


        private async Task<T?> SendRequestAsync<T>(HttpRequestMessage request, string? jwtToken = null)
        {
            AddJwt(request, jwtToken);

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reach Product MS");
                throw new CustomException.ProductServiceConnectionException(
                    "Failed to reach Product MS", ex);
            }

            var rawBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Product MS returned error. Status: {Status}, Body: {Body}",
                    response.StatusCode,
                    rawBody);

                throw new CustomException.ProductServiceResponseException(
                    (int)response.StatusCode,
                    rawBody);
            }

            
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return default; 
            }

            try
            {
                var result = JsonSerializer.Deserialize<T>(rawBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                    throw new JsonException("Deserialized result is null");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to deserialize Product MS response. StatusCode: {StatusCode}, Body: {Body}",
                    response.StatusCode,
                    rawBody);

                throw new CustomException.ProductServiceDeserializationException(
                    "Failed to deserialize Product MS response", ex);
            }
        }


        public async Task<ProductListDTO> GetFilteredProducts(ProductFilterParameters query)
        {
            var page = query.Page ?? 1;
            var pageSize = query.PageSize ?? 20;

            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 20 : pageSize;

            var queryParams = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(query.Search) && query.Search != "string")
                queryParams["search"] = query.Search.Trim();

            if (query.CategoryId.HasValue && query.CategoryId.Value > 0)
                queryParams["categoryId"] = query.CategoryId.Value.ToString();

            if (query.MinPrice.HasValue)
                queryParams["minPrice"] = query.MinPrice.Value.ToString();

            if (query.MaxPrice.HasValue)
                queryParams["maxPrice"] = query.MaxPrice.Value.ToString();

            if (query.Available.HasValue)
                queryParams["available"] = query.Available.Value.ToString().ToLower(); 

            
            queryParams["page"] = page.ToString();
            queryParams["pageSize"] = pageSize.ToString();

            var queryString = QueryString.Create(queryParams).ToUriComponent();

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/products/filter{queryString}"
            );

            _logger.LogInformation("Sending request to Product MS: {Url}", request.RequestUri);

            var product = await SendRequestAsync<ProductListDTO>(request);
            return product!;
        }


        public async Task<ProductDTO?> GetProductByIdAsync(int productId, string? jwtToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/products/{productId}");
            AddJwt(request, jwtToken);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reach Product MS");
                throw new CustomException.ProductServiceConnectionException("Failed to reach Product MS", ex);
            }

            var rawBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Product with Id {Id} not found", productId);
                return null; 
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Product MS returned {Status} for product {Id}: {Body}", response.StatusCode, productId, rawBody);
                throw new CustomException.ProductServiceResponseException((int)response.StatusCode, rawBody);
            }

            try
            {
                var product = JsonSerializer.Deserialize<ProductDTO>(rawBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (product == null)
                    throw new JsonException("Deserialized product is null");

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize Product MS response. Body: {Body}", rawBody);
                throw new CustomException.ProductServiceDeserializationException("Failed to deserialize Product MS response", ex);
            }
        }


        public async Task<ProductListDTO> GetAllAsync(string? jwtToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/products/All");
            var wrapper = await SendRequestAsync<ProductListDTO>(request, jwtToken);
            _logger.LogInformation("Products were Successfully retrieved!" + wrapper);

            var allUsers = await _userService.GetAllUsersAsync();
            var activeUserIds=  allUsers
                .Where(u => u.IsActive)
                .Select(u => u.UserId)
                .ToHashSet();

            wrapper.Items = wrapper.Items.Where(p => activeUserIds.Contains(p.UserId)).ToList();
            wrapper.TotalCount = wrapper.Items.Count();
            return wrapper;
        }

        public async Task<ProductListDTO> GetByNameAsync(string name, string? jwtToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/products/Search?name={name}");
            var wrapper = await SendRequestAsync<ProductListDTO>(request, jwtToken);

            var allUsers = await _userService.GetAllUsersAsync();
            var activeUserIds = allUsers
                .Where(u => u.IsActive)
                .Select(u => u.UserId)
                .ToHashSet();

            wrapper.Items = wrapper.Items.Where(p => activeUserIds.Contains(p.UserId)).ToList();
            wrapper.TotalCount = wrapper.Items.Count();

            _logger.LogInformation($"Products with search={name} were successfull!");
            return wrapper  ;
        }

        public async Task<ProductListDTO> GetForUserAsync(string? jwtToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/products/MyProducts");
            var wrapper = await SendRequestAsync<ProductListDTO>(request, jwtToken);
            _logger.LogInformation($"Products were Successfully retrieved for user!");
            return wrapper;
        }

        public async Task UpdateAsync(int productId, UpdateProductDTO dto, string? jwtToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/products/Update/{productId}")
            {
                Content = JsonContent.Create(dto)
            };


            
            await SendRequestAsync<object>(request, jwtToken);
            _logger.LogInformation($"the Product with Id {productId} was successfully updated!");
        }

        public async Task DeleteAsync(int productId, string? jwtToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/products/Delete/{productId}");

            await SendRequestAsync<object>(request, jwtToken);
            _logger.LogInformation($"Product with id {productId} was Successfully deleted!");
        }

        public async Task AddImageAsync(int productId, List<IFormFile> images, string? jwtToken = null)
        {

            var content = new MultipartFormDataContent();

            foreach (var file in images)
            {
                if (file.Length == 0)
                    continue;

                var streamContent = new StreamContent(file.OpenReadStream());
                streamContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

                content.Add(
                    streamContent,
                    "images",          
                    file.FileName
                );
            }

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/products/{productId}/images")
            {
                Content = content
            };

            AddJwt(request, jwtToken);

            await SendRequestAsync<object>(request, jwtToken);

            _logger.LogInformation(
                "Images forwarded successfully to Product MS for product {ProductId}",
                productId
            );
        }

        public async Task RemoveImageAsync(int imageId,int productId,  string? jwtToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/products/{productId}/images/{imageId}");
            await SendRequestAsync<object>(request, jwtToken);
            _logger.LogInformation($"image with id {imageId} of product {productId} was Successfully deleted!");
        }

        public async Task<ProductDTO> CreateAsync(CreateProductDTO dto, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/products/Create")
            {
                Content = JsonContent.Create(dto)
            };

            AddJwt(request, jwt);

            var product = await SendRequestAsync<ProductDTO>(request, jwt);

            return product!;
        }
    }
}
