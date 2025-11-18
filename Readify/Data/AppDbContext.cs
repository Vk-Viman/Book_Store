using Microsoft.EntityFrameworkCore;
using Readify.Models;

namespace Readify.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<UserProfileUpdate> UserProfileUpdates { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<ShippingSetting> ShippingSettings { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<OrderHistory> OrderHistories { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<PromoCodeUsage> PromoCodeUsages { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        // Alias for readability: treat products as books in the app domain
        public DbSet<Product> Books => Products;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>()
                .HasMany(c => c.Children)
                .WithOne(c => c.Parent)
                .HasForeignKey(c => c.ParentId);

            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId);

            // Product -> ProductImages mapping
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Images)
                .WithOne(pi => pi.Product)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => new { pi.ProductId, pi.SortOrder });

            // Ensure Price uses decimal(18,2) in SQL Server to avoid truncation
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // Ensure order decimal precision
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18,2);

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.UnitPrice)
                .HasPrecision(18,2);

            // Concurrency token
            modelBuilder.Entity<Product>()
                .Property(p => p.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Title);
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.CategoryId);
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Price);
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.AvgRating);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PromoCode>()
                .Property(p => p.DiscountPercent)
                .HasColumnType("decimal(5,2)");

            modelBuilder.Entity<PromoCode>()
                .Property(p => p.FixedAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PromoCode>()
                .Property(p => p.Type)
                .HasMaxLength(32);

            // Ensure discount and shipping amounts use appropriate types
            modelBuilder.Entity<Order>()
                .Property(o => o.DiscountAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.DiscountPercent)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.ShippingCost)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ShippingSetting>()
                .Property(s => s.Local)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<ShippingSetting>()
                .Property(s => s.National)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<ShippingSetting>()
                .Property(s => s.International)
                .HasColumnType("decimal(18,2)");
            modelBuilder.Entity<ShippingSetting>()
                .Property(s => s.FreeShippingThreshold)
                .HasColumnType("decimal(18,2)");

            // Configure wishlist
            modelBuilder.ConfigureWishlist();

            modelBuilder.Entity<OrderHistory>()
                .HasIndex(h => h.OrderId);

            modelBuilder.Entity<Review>()
                .HasIndex(r => r.ProductId);

            // configure AvgRating precision
            modelBuilder.Entity<Product>()
                .Property(p => p.AvgRating)
                .HasPrecision(3,2);

            modelBuilder.Entity<Order>()
                .Property(o => o.OriginalTotal)
                .HasPrecision(18,2);

            modelBuilder.Entity<PromoCodeUsage>()
                .HasIndex(u => new { u.PromoCodeId, u.UserId });
            modelBuilder.Entity<PromoCodeUsage>()
                .HasIndex(u => u.PromoCodeId);

            modelBuilder.Entity<PromoCode>()
                .Property(p => p.MinPurchase)
                .HasColumnType("decimal(18,2)");
        }
    }
}
