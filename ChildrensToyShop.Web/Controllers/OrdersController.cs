using ChildrensToyShop.Web.Data;
using ChildrensToyShop.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChildrensToyShop.Web.Controllers;

[Authorize]
public class OrdersController(ApplicationDbContext db) : Controller
{
    private async Task<ShoppingCart?> CartAsync() => await db.Carts.Include(x => x.Items).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.UserId == User.CurrentUserId());
    public async Task<IActionResult> Index()
    {
        var orders = db.Orders.Include(x => x.User).Include(x => x.Items).Where(x => !x.IsArchived);
        if (!User.IsAdmin()) orders = orders.Where(x => x.UserId == User.CurrentUserId());
        return View(await orders.OrderByDescending(x => x.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await db.Orders.Include(x => x.User).Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id);
        if (order is null || (!User.IsAdmin() && order.UserId != User.CurrentUserId())) return NotFound();
        return View(order);
    }

    public async Task<IActionResult> Checkout()
    {
        var cart = await CartAsync(); if (cart is null || cart.Items.Count == 0) { TempData["Error"] = "Your cart is empty."; return RedirectToAction("Index", "Cart"); }
        return View(new CheckoutViewModel { Cart = new CartViewModel { Cart = cart } });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        var cart = await CartAsync(); model.Cart = new CartViewModel { Cart = cart ?? new ShoppingCart() };
        if (cart is null || cart.Items.Count == 0) { ModelState.AddModelError(string.Empty, "Your cart is empty."); return View(model); }
        foreach (var line in cart.Items)
            if (line.Product is null || !line.Product.IsActive || line.Product.Status != ProductStatus.Active || line.Quantity > line.Product.StockQuantity) ModelState.AddModelError(string.Empty, $"{line.Product?.Title ?? "An item"} is no longer available in the requested quantity.");
        if (!ModelState.IsValid) return View(model);
        var totals = model.Cart;
        var order = new Order { UserId = User.CurrentUserId(), OrderNumber = $"TOY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}", ShippingAddress = model.ShippingAddress, Status = OrderStatus.Paid, Subtotal = totals.Subtotal, Tax = totals.Tax, GiftWrapFee = totals.GiftWrapFee, Total = totals.Total };
        foreach (var line in cart.Items)
        {
            var p = line.Product!; p.StockQuantity -= line.Quantity;
            order.Items.Add(new OrderItem { ProductId = p.Id, ProductTitle = p.Title, Sku = p.Sku, UnitPrice = p.Price, Quantity = line.Quantity, GiftWrap = line.GiftWrap });
        }
        db.Orders.Add(order); db.CartItems.RemoveRange(cart.Items); await db.SaveChangesAsync();
        TempData["Success"] = "Payment simulation accepted. Your order is now PAID."; return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? trackingNumber)
    {
        var order = await db.Orders.FindAsync(id); if (order is null) return NotFound();
        var allowed = new[] { OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered };
        if (!allowed.Contains(status)) { TempData["Error"] = "Invalid fulfillment status."; return RedirectToAction(nameof(Details), new { id }); }
        order.Status = status; if (!string.IsNullOrWhiteSpace(trackingNumber)) order.TrackingNumber = trackingNumber.Trim(); await db.SaveChangesAsync(); TempData["Success"] = "Order fulfillment updated."; return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id);
        if (order is null || (!User.IsAdmin() && order.UserId != User.CurrentUserId())) return NotFound();
        if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled) { TempData["Error"] = "Only unshipped orders can be cancelled."; return RedirectToAction(nameof(Details), new { id }); }
        foreach (var item in order.Items) { var product = await db.Products.FindAsync(item.ProductId); if (product is not null) product.StockQuantity += item.Quantity; }
        order.Status = OrderStatus.Cancelled; order.IsArchived = true; await db.SaveChangesAsync(); TempData["Success"] = "Order cancelled, payment simulation voided, and stock restored."; return RedirectToAction(nameof(Index));
    }
}
