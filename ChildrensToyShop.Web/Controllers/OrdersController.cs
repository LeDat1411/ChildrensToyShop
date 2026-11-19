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
        var cart = await CartAsync();
        if (cart is null || cart.Items.Count == 0)
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        model.Cart = new CartViewModel { Cart = cart };
        if (cart.CouponCode != model.ExpectedCouponCode)
            ModelState.AddModelError(string.Empty, "Your coupon selection changed. Review the total before placing your order.");

        foreach (var line in cart.Items)
        {
            if (line.Product is null || !line.Product.IsActive || line.Product.Status != ProductStatus.Active || line.Quantity > line.Product.StockQuantity)
                ModelState.AddModelError(string.Empty, $"{line.Product?.Title ?? "An item"} is no longer available in the requested quantity.");
        }
        if (!ModelState.IsValid) return View(model);

        var totals = model.Cart;
        var coupon = totals.AppliedCoupon;
        var order = new Order
        {
            UserId = cart.UserId,
            OrderNumber = $"TOY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..10]}",
            ShippingAddress = model.ShippingAddress,
            Status = OrderStatus.Paid,
            Subtotal = totals.Subtotal,
            Tax = totals.Tax,
            GiftWrapFee = totals.GiftWrapFee,
            CouponCode = coupon?.Code,
            DiscountAmount = totals.DiscountAmount,
            Total = totals.Total
        };

        await using var transaction = await db.Database.BeginTransactionAsync();
        foreach (var line in cart.Items.OrderBy(x => x.ProductId))
        {
            var product = line.Product!;
            var updated = await db.Products.Where(x => x.Id == product.Id && x.IsActive &&
                    x.Status == ProductStatus.Active && x.StockQuantity >= line.Quantity && x.Price == product.Price)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.StockQuantity, x => x.StockQuantity - line.Quantity));
            if (updated == 0)
            {
                TempData["Error"] = $"{product.Title} changed while you were checking out. Review your cart and try again.";
                return RedirectToAction("Index", "Cart");
            }

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id, ProductTitle = product.Title, Sku = product.Sku,
                UnitPrice = product.Price, Quantity = line.Quantity, GiftWrap = line.GiftWrap
            });
        }

        if (coupon is not null)
        {
            var redeemed = await db.CustomerCoupons.Where(x =>
                    x.UserId == order.UserId && x.Code == coupon.Code && x.Quantity > 0)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Quantity, x => x.Quantity - 1));
            if (redeemed == 0)
            {
                TempData["Error"] = $"Coupon {coupon.Code} is unavailable. Change or remove it in My Cart.";
                return RedirectToAction("Index", "Cart");
            }
        }

        cart.CouponCode = null;
        cart.UpdatedAt = DateTime.UtcNow;
        db.Orders.Add(order);
        db.CartItems.RemoveRange(cart.Items);
        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "Your cart changed during checkout. Review it before placing your order.";
            return RedirectToAction("Index", "Cart");
        }

        TempData["Success"] = "Payment simulation accepted. Your order is now PAID.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? trackingNumber)
    {
        var order = await db.Orders.FindAsync(id); if (order is null) return NotFound();
        var allowed = new[] { OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered };
        if (!allowed.Contains(status) || order.Status == OrderStatus.Cancelled) { TempData["Error"] = "Invalid fulfillment status."; return RedirectToAction(nameof(Details), new { id }); }
        var tracking = string.IsNullOrWhiteSpace(trackingNumber) ? order.TrackingNumber : trackingNumber.Trim();
        var updated = await db.Orders.Where(x => x.Id == id && x.Status == order.Status)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, status)
                .SetProperty(x => x.TrackingNumber, tracking));
        if (updated == 0) TempData["Error"] = "The order changed. Review its current status.";
        else TempData["Success"] = "Order fulfillment updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await db.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id);
        if (order is null || (!User.IsAdmin() && order.UserId != User.CurrentUserId())) return NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var cancelled = await db.Orders.Where(x => x.Id == id &&
                (x.Status == OrderStatus.Paid || x.Status == OrderStatus.Processing))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, OrderStatus.Cancelled)
                .SetProperty(x => x.IsArchived, true));
        if (cancelled == 0)
        {
            TempData["Error"] = "Only unshipped orders can be cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        foreach (var item in order.Items.OrderBy(x => x.ProductId))
        {
            await db.Products.Where(x => x.Id == item.ProductId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.StockQuantity, x => x.StockQuantity + item.Quantity));
        }

        if (order.CouponCode is not null)
        {
            await db.CustomerCoupons.Where(x => x.UserId == order.UserId && x.Code == order.CouponCode)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Quantity, x => x.Quantity + 1));
        }

        await transaction.CommitAsync();
        TempData["Success"] = order.CouponCode is null
            ? "Order cancelled, payment simulation voided, and stock restored."
            : $"Order cancelled. Stock and coupon {order.CouponCode} were restored.";
        return RedirectToAction(nameof(Index));
    }
}
