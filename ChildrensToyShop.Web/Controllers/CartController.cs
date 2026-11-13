using ChildrensToyShop.Web.Data;
using ChildrensToyShop.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChildrensToyShop.Web.Controllers;

[Authorize]
public class CartController(ApplicationDbContext db) : Controller
{
    private async Task<ShoppingCart> CurrentCartAsync()
    {
        var userId = User.CurrentUserId();
        var cart = await db.Carts.Include(x => x.Items).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.UserId == userId);
        if (cart is not null) return cart;
        cart = new ShoppingCart { UserId = userId }; db.Carts.Add(cart); await db.SaveChangesAsync(); return cart;
    }

    public async Task<IActionResult> Index() => View(new CartViewModel { Cart = await CurrentCartAsync() });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, string? returnUrl)
    {
        var product = await db.Products.FindAsync(productId);
        if (product is null || !product.IsActive || product.Status != ProductStatus.Active) { TempData["Error"] = "SALE PROHIBITED: this toy is recalled, discontinued, or unavailable."; return Redirect(returnUrl ?? "/Products"); }
        var cart = await CurrentCartAsync(); var line = cart.Items.SingleOrDefault(x => x.ProductId == productId);
        if ((line?.Quantity ?? 0) >= product.StockQuantity) { TempData["Error"] = "There is not enough available stock for that quantity."; return Redirect(returnUrl ?? "/Products"); }
        if (line is null) db.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = productId }); else line.Quantity++;
        cart.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(); TempData["Success"] = "Toy added to your cart."; return Redirect(returnUrl ?? "/Cart");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int itemId, int quantity, bool giftWrap)
    {
        var cart = await CurrentCartAsync(); var item = cart.Items.SingleOrDefault(x => x.Id == itemId); if (item is null) return NotFound();
        if (quantity <= 0) db.CartItems.Remove(item);
        else if (item.Product is not null && quantity > item.Product.StockQuantity) TempData["Error"] = "Quantity exceeds current stock.";
        else { item.Quantity = quantity; item.GiftWrap = giftWrap; cart.UpdatedAt = DateTime.UtcNow; }
        await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int itemId)
    {
        var cart = await CurrentCartAsync(); var item = cart.Items.SingleOrDefault(x => x.Id == itemId); if (item is not null) db.CartItems.Remove(item);
        await db.SaveChangesAsync(); TempData["Success"] = "Item removed from your cart."; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        var cart = await CurrentCartAsync(); db.CartItems.RemoveRange(cart.Items); await db.SaveChangesAsync(); TempData["Success"] = "Your cart is now empty."; return RedirectToAction(nameof(Index));
    }
}
