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

    public async Task<IActionResult> Index()
    {
        var cart = await CurrentCartAsync();
        var coupons = User.IsInRole("Customer")
            ? await db.CustomerCoupons.AsNoTracking().Where(x => x.UserId == cart.UserId).ToListAsync()
            : [];
        return View(new CartViewModel { Cart = cart, Coupons = coupons });
    }

    [Authorize(Roles = "Customer"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCoupon(string? couponCode)
    {
        var cart = await CurrentCartAsync();
        if (cart.Items.Count == 0) return RedirectToAction(nameof(Index));

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var coupon = CouponType.Find(couponCode.Trim().ToUpperInvariant());
            if (coupon is null || !await db.CustomerCoupons.AnyAsync(x =>
                x.UserId == cart.UserId && x.Code == coupon.Code && x.Quantity > 0))
            {
                TempData["Error"] = "That coupon is unavailable in your account.";
                return RedirectToAction(nameof(Index));
            }

            cart.CouponCode = coupon.Code;
            TempData["Success"] = $"Coupon {coupon.Code} applied for {coupon.Percent}% off.";
        }
        else
        {
            cart.CouponCode = null;
            TempData["Success"] = "Coupon removed from your cart.";
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await SaveCartAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, string? returnUrl)
    {
        var product = await db.Products.FindAsync(productId);
        if (product is null || !product.IsActive || product.Status != ProductStatus.Active) { TempData["Error"] = "SALE PROHIBITED: this toy is recalled, discontinued, or unavailable."; return Redirect(returnUrl ?? "/Products"); }
        var cart = await CurrentCartAsync(); var line = cart.Items.SingleOrDefault(x => x.ProductId == productId);
        if ((line?.Quantity ?? 0) >= product.StockQuantity) { TempData["Error"] = "There is not enough available stock for that quantity."; return Redirect(returnUrl ?? "/Products"); }
        if (line is null) db.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = productId }); else line.Quantity++;
        cart.UpdatedAt = DateTime.UtcNow;
        TempData["Success"] = "Toy added to your cart.";
        await SaveCartAsync();
        return Redirect(returnUrl ?? "/Cart");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int itemId, int quantity, bool giftWrap)
    {
        var cart = await CurrentCartAsync(); var item = cart.Items.SingleOrDefault(x => x.Id == itemId); if (item is null) return NotFound();
        if (quantity <= 0) RemoveLine(cart, item);
        else if (item.Product is not null && quantity > item.Product.StockQuantity) TempData["Error"] = "Quantity exceeds current stock.";
        else { item.Quantity = quantity; item.GiftWrap = giftWrap; cart.UpdatedAt = DateTime.UtcNow; }
        await SaveCartAsync(); return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int itemId)
    {
        var cart = await CurrentCartAsync(); var item = cart.Items.SingleOrDefault(x => x.Id == itemId); if (item is not null) RemoveLine(cart, item);
        TempData["Success"] = "Item removed from your cart.";
        await SaveCartAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        var cart = await CurrentCartAsync();
        cart.CouponCode = null;
        cart.UpdatedAt = DateTime.UtcNow;
        db.CartItems.RemoveRange(cart.Items);
        TempData["Success"] = "Your cart is now empty.";
        await SaveCartAsync();
        return RedirectToAction(nameof(Index));
    }

    private void RemoveLine(ShoppingCart cart, CartItem item)
    {
        if (cart.Items.Count == 1) cart.CouponCode = null;
        cart.UpdatedAt = DateTime.UtcNow;
        db.CartItems.Remove(item);
    }

    private async Task SaveCartAsync()
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData.Remove("Success");
            TempData["Error"] = "Your cart changed in another request. Review it and try again.";
        }
    }
}
