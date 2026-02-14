namespace ProductManagement.Models.Data
{
    public class Product
    {
        public int ProductId { get; set; } //primary key
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Decimal Price { get; set; }
        public int Amount { get; set; }
        public int UserId { get; set; } // NOT foreign key 
        public int CategoryId { get; set; } //foreign Key
        public DateTime CreationDate { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        public ProductCategory? ProductCategory { get; set; }
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    }
}
