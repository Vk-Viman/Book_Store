using Microsoft.EntityFrameworkCore;
using Readify.Models;

namespace Readify.Data
{
    public static class WishlistDbExtensions
    {
        public static void ConfigureWishlist(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Wishlist>()
                .HasKey(w => new { w.UserId, w.ProductId });

            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
