namespace ProductManagement.Models.DTOs
{
    public class UpdateProductDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Decimal Price { get; set; }
        public int Amount { get; set; }
        
    }
}
