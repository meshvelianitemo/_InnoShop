using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
namespace ProductManagement.Models.Data
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
        {

        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductCategory>(entity =>
            {
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.CategoryName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasData(
                  new ProductCategory { CategoryId = 1, CategoryName = "Electronics" },
                  new ProductCategory { CategoryId = 2, CategoryName = "Clothing" },
                  new ProductCategory { CategoryId = 3, CategoryName = "Home & Kitchen" },
                  new ProductCategory { CategoryId = 4, CategoryName = "Books" },
                  new ProductCategory { CategoryId = 5, CategoryName = "Sports & Outdoors" },
                  new ProductCategory { CategoryId = 6, CategoryName = "Beauty & Personal Care" },
                  new ProductCategory { CategoryId = 7, CategoryName = "Toys & Games" }
            );
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductId);
                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(200);
                entity.Property(e => e.Description)
                      .HasMaxLength(1000);
                entity.Property(e => e.Price)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();
                entity.Property(e => e.Amount)
                      .IsRequired();
                entity.HasOne(p=> p.ProductCategory)  //Foregin key relationship with ProductCategory
                      .WithMany(c => c.Products)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e=> e.UserId)
                      .IsRequired();
                entity.Property(e => e.CreationDate);
                entity.Property(e => e.ModifiedDate);

                entity.HasMany(p => p.ProductImages) 
                  .WithOne(pi => pi.Product)
                  .HasForeignKey(pi => pi.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<ProductImage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Product) //Foreign key relationship with Product
                      .WithMany(p => p.ProductImages)
                      .HasForeignKey(p => p.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.ImagePath)
                      .IsRequired()
                      .HasMaxLength(500);
            });


        }
    }
}
