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
        }
    }
}
