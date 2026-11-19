using ChildrensToyShop.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ChildrensToyShop.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ShoppingCart> Carts => Set<ShoppingCart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<CustomerCoupon> CustomerCoupons => Set<CustomerCoupon>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        modelBuilder.Entity<Product>().Property(x => x.SafetyNotes).IsRequired();
        modelBuilder.Entity<Product>().Property(x => x.SafetyCertification).IsRequired();
        modelBuilder.Entity<ShoppingCart>().HasIndex(x => x.UserId).IsUnique();
        modelBuilder.Entity<ShoppingCart>().Property(x => x.UpdatedAt).IsConcurrencyToken();
        modelBuilder.Entity<CustomerCoupon>().HasKey(x => new { x.UserId, x.Code });
        modelBuilder.Entity<CustomerCoupon>().ToTable(table =>
        {
            table.HasCheckConstraint("CK_CustomerCoupons_Code", "[Code] IN ('A', 'B', 'C')");
            table.HasCheckConstraint("CK_CustomerCoupons_Quantity", "[Quantity] >= 0");
        });
        modelBuilder.Entity<CartItem>().HasIndex(x => new { x.CartId, x.ProductId }).IsUnique();
        modelBuilder.Entity<Product>().Property(x => x.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(x => x.Subtotal).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(x => x.Tax).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(x => x.GiftWrapFee).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(x => x.DiscountAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(x => x.Total).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Review>().HasIndex(x => new { x.ProductId, x.UserId }).IsUnique();
    }
}
