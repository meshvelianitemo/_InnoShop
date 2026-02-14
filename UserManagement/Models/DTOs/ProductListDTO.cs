using System.Text.Json.Serialization;

namespace UserManagement.Models.DTOs
{
    public class ProductListDTO
    {
        [JsonPropertyName("items")]
        public IEnumerable<ProductDTO> Items { get; set; } = new List<ProductDTO>();

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

    }
}
