namespace ProductManagement.Models.DTOs
{
    public class ProductResponseDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Amount { get; set; }

        // This comes from ProductImages table
        public List<string>? ImagePaths { get; set; }

        // these so User MS sees them
        public int UserId { get; set; }
        public int CategoryId { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
