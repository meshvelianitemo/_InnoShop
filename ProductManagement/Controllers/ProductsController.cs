using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ProductManagement.Exceptions;
using ProductManagement.Models.Data;
using ProductManagement.Models.DTOs;
using ProductManagement.Services;
using System.Security.Claims;

namespace ProductManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        
        public ProductsController(IProductService productService)
        {      
            _productService = productService;
        }


        [HttpGet("All")]
        public async Task<IActionResult> Products([FromQuery] PaginationParameters pagination)
        {
            var query = new ProductQueryParameters()
            {
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                Available = true
            };
            var products = await _productService.GetAllAsync(query);
            return Ok(products);
        }

        [HttpGet("Search")]
        public async Task<IActionResult> GetProductByName([FromQuery] string name, [FromQuery] PaginationParameters pagination )
        {
            var query = new ProductQueryParameters()
            {
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                Available = true,
                Search = name
            };
            var products = await _productService.GetAllAsync(query);
            return Ok(products);
        }


        [HttpGet("MyProducts")]
        [Authorize]
        public async Task<IActionResult> GetForUser([FromQuery] ProductQueryParameters query)
        {
            var userId = GetUserIdFromClaims();
            var result = await _productService.GetAllForUserAsync(userId, query);
            return Ok(result);
        }

        [HttpGet("{productId}")]
        public async Task<IActionResult> GetById(int productId)
        {
            var product = await _productService.GetByIdAsync(productId);
            return Ok(product);
        }


        [HttpGet("Filter")]
        public async Task<IActionResult> Filter([FromQuery] ProductQueryParameters query)
        {
            var products = await _productService.GetAllAsync(query);
            return Ok(products);
        }
        
        
        [HttpPut("Update/{productId}")]
        [Authorize]
        public async Task<IActionResult> Update(int productId, [FromBody] UpdateProductDTO dto)
        {
            var userId = GetUserIdFromClaims();
            await _productService.UpdateAsync(productId, dto, userId);
            return NoContent();
        }



        [HttpPost("Create")]
        [Authorize(Roles = "Client,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateProductDTO dto)
        {
            var userId = GetUserIdFromClaims();
            var product = await _productService.CreateAsync(dto, userId);
            return Ok(product);
        }



        [HttpPost("{productId}/images")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddImage(int productId, [FromForm] List<IFormFile> images)
        {
            if (images == null || images.Count == 0)
            {
                return BadRequest("No images uploaded.");
            }

            var userId = GetUserIdFromClaims();
            await _productService.AddImageAsync(productId, images, userId);
            return NoContent();
        }

        [HttpDelete("Delete/{productId}")]
        [Authorize]
        public async Task<IActionResult> Delete(int productId)
        {
            var userId = GetUserIdFromClaims();
            await _productService.DeleteAsync(productId, userId);
            return NoContent();
        }

        [HttpDelete("{productId}/images/{imageId}")]
        [Authorize]
        public async Task<IActionResult> RemoveImage(int imageId, int productId)
        {
            var userId = GetUserIdFromClaims();
            await _productService.RemoveImageAsync(imageId, userId, productId);
            return NoContent();
        }


        private int GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user ID in token");
            return userId;
        }

    }
}
