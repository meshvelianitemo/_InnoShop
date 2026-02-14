namespace ProductManagement.Models.Data
{
    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; } 
        public string ImagePath { get; set; } = string.Empty;

        public Product Product { get; set; } = null!;
    }
}
