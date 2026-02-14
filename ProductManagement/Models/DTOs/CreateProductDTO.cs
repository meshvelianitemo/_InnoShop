namespace ProductManagement.Models.DTOs
{
    public class CreateProductDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Decimal Price { get; set; }  
        public int Amount { get; set; }
        public string CategoryName { get; set; } = string.Empty;

    }
}
