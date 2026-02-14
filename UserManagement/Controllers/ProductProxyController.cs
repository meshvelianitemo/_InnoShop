using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Models.DTOs;
using UserManagement.Models.DTOs;
using UserManagement.Services;

namespace UserManagement.Controllers
{

    [ApiController]
    [Route("api/products-proxy")]
    public class ProductProxyController : ControllerBase
    {
        private readonly IProductServiceClient _productServiceClient;
        private readonly ILogger<ProductProxyController> _logger;
             
        public ProductProxyController(IProductServiceClient productServiceClient, ILogger<ProductProxyController> logger)
        {
            _productServiceClient = productServiceClient;
            _logger = logger;
        }

        private string? GetJwtFromCookie()
        {
            return HttpContext.Request.Cookies["JwtToken"];
        }


        // GET api/products-proxy/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            // Fetch product from Product MS via HttpClient
            var product = await _productServiceClient.GetProductByIdAsync(id);

            if (product == null)
                return NotFound();

            return Ok(product);
        }

        // GET api/products-proxy/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _productServiceClient.GetAllAsync();
            return Ok(products);
        }

        // GET api/products-proxy/search?name=...
        [HttpGet("search")]
        public async Task<IActionResult> GetByName([FromQuery] string name)
        {
            var products = await _productServiceClient.GetByNameAsync(name);
            return Ok(products);
        }

        // GET api/products-proxy/mine
        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> GetForUser()
        {
            var jwt = GetJwtFromCookie();
            var products = await _productServiceClient.GetForUserAsync(jwt);
            return Ok(products);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> Filter([FromQuery] ProductFilterParameters query)
        {
            var products = await _productServiceClient.GetFilteredProducts(query);
            return Ok(products);
        }

        // PUT api/products-proxy/{id}
        [HttpPut("{Productid}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(int Productid, [FromBody] UpdateProductDTO dto)
        {
            var jwt = GetJwtFromCookie();
            await _productServiceClient.UpdateAsync(Productid, dto, jwt);
            return NoContent();
        }

        // POST api/products-proxy/create
        [HttpPost("Create")]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateProductDTO dto)
        {
           var jwt = GetJwtFromCookie();
            if (string.IsNullOrEmpty(jwt))
                return Unauthorized();

            var createdProduct = await _productServiceClient.CreateAsync(dto, jwt);

            return Ok(new { message = "the product was uploaded sucessfully!" });

        }


        // POST api/products-proxy/{id}/images
        [HttpPost("{id}/images")]
        [Authorize]
        public async Task<IActionResult> AddImage(int id, [FromForm] List<IFormFile> images)
        {
            var jwt = GetJwtFromCookie();
            await _productServiceClient.AddImageAsync(id, images, jwt);
            return NoContent();
        }


        // DELETE api/products-proxy/{id}
        [HttpDelete("{productId}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            var jwt = GetJwtFromCookie();
            await _productServiceClient.DeleteAsync(productId, jwt);
            return NoContent();
        }

        

        // DELETE api/products-proxy/images/{imageId}
        [HttpDelete("images/{imageId}")]
        [Authorize]
        public async Task<IActionResult> RemoveImage(int imageId, int productId)
        {
            var jwt = GetJwtFromCookie();
            await _productServiceClient.RemoveImageAsync(imageId, productId, jwt);
            return NoContent();
        }
    }
}
