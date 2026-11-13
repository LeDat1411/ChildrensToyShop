using System.ComponentModel.DataAnnotations;

namespace ChildrensToyShop.Web.Models;

public class AppUser
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string DisplayName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(64)] public string PasswordHash { get; set; } = string.Empty;
    [Required, StringLength(20)] public string Role { get; set; } = "Customer";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class Product
{
    public int Id { get; set; }
    [Required, StringLength(40)] public string Sku { get; set; } = string.Empty;
    [Required, StringLength(140)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Description { get; set; } = string.Empty;
    [Range(0.01, 999999)] public decimal Price { get; set; }
    [Required, StringLength(40)] public string AgeRange { get; set; } = string.Empty;
    [Required, StringLength(60)] public string Category { get; set; } = string.Empty;
    [Required, StringLength(60)] public string Brand { get; set; } = string.Empty;
    [StringLength(240)] public string SafetyNotes { get; set; } = string.Empty;
    [StringLength(100)] public string SafetyCertification { get; set; } = string.Empty;
    [Range(0, 999999)] public int StockQuantity { get; set; }
    [Range(0, 999999)] public int ReorderThreshold { get; set; } = 10;
    [Required, StringLength(20)] public string Status { get; set; } = ProductStatus.Active;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public static class ProductStatus { public const string Active = "Active"; public const string Recalled = "Recalled"; public const string Discontinued = "Discontinued"; }

public class ShoppingCart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public ShoppingCart? Cart { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [Range(1, 99)] public int Quantity { get; set; } = 1;
    public bool GiftWrap { get; set; }
}

public class Order
{
    public int Id { get; set; }
    [Required, StringLength(30)] public string OrderNumber { get; set; } = string.Empty;
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    [Required, StringLength(20)] public string Status { get; set; } = OrderStatus.Paid;
    [Required, StringLength(300)] public string ShippingAddress { get; set; } = string.Empty;
    [StringLength(80)] public string? TrackingNumber { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal GiftWrapFee { get; set; }
    public decimal Total { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int ProductId { get; set; }
    [Required, StringLength(140)] public string ProductTitle { get; set; } = string.Empty;
    [Required, StringLength(40)] public string Sku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public bool GiftWrap { get; set; }
}

public static class OrderStatus { public const string Paid = "PAID"; public const string Processing = "PROCESSING"; public const string Shipped = "SHIPPED"; public const string Delivered = "DELIVERED"; public const string Cancelled = "CANCELLED"; }

public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    [Range(1, 5)] public int Rating { get; set; }
    [Required, StringLength(1600)] public string Comment { get; set; } = string.Empty;
    [Required, StringLength(40)] public string ActualAgeSuitability { get; set; } = string.Empty;
    [Required, StringLength(20)] public string VisibilityStatus { get; set; } = ReviewStatus.Approved;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class ReviewStatus { public const string Pending = "PENDING"; public const string Approved = "APPROVED"; public const string Flagged = "FLAGGED"; }
