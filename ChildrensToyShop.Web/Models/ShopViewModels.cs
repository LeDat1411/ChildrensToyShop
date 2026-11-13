using System.ComponentModel.DataAnnotations;

namespace ChildrensToyShop.Web.Models;

public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public class ProductListViewModel
{
    public IEnumerable<Product> Products { get; set; } = [];
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? AgeRange { get; set; }
    public string? Brand { get; set; }
    public bool InStock { get; set; }
    public IEnumerable<string> Categories { get; set; } = [];
    public IEnumerable<string> AgeRanges { get; set; } = [];
    public IEnumerable<string> Brands { get; set; } = [];
}

public class CartViewModel
{
    public ShoppingCart Cart { get; set; } = new();
    public decimal Subtotal => Cart.Items.Sum(x => (x.Product?.Price ?? 0) * x.Quantity);
    public decimal GiftWrapFee => Cart.Items.Where(x => x.GiftWrap).Sum(x => 2.50m * x.Quantity);
    public decimal Tax => Math.Round((Subtotal + GiftWrapFee) * .10m, 2);
    public decimal Total => Subtotal + GiftWrapFee + Tax;
}

public class CheckoutViewModel
{
    [Required, StringLength(300), Display(Name = "Shipping address")]
    public string ShippingAddress { get; set; } = string.Empty;
    public CartViewModel Cart { get; set; } = new();
}
