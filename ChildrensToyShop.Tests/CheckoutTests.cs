using ChildrensToyShop.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace ChildrensToyShop.Tests;

public sealed partial class ShopSystemTests
{
    [Fact]
    public async Task TC13_Simulated_checkout_saves_two_items_updates_stock_and_shows_receipt()
    {
        await Login();
        var first = await Toy(MagneticSet);
        var second = await Toy(Explorer);
        await AddToCart(MagneticSet);
        await AddToCart(Explorer);
        await PlaceOrder();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Order details" })).ToBeVisibleAsync();
        await Expect(page.Locator(".order-line")).ToHaveCountAsync(2);
        await Expect(page.Locator(".order-line").First).ToContainTextAsync("Qty 1");
        await Expect(page.Locator("dd.total")).ToHaveTextAsync("$90.19");
        await using var db = OpenDatabase();
        var order = await db.Orders.Include(o => o.Items).SingleAsync();
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(90.19m, order.Total);
        Assert.Equal(new[] { first.Id, second.Id }.Order(), order.Items.Select(i => i.ProductId).Order());
        Assert.All(order.Items, i => Assert.Equal(1, i.Quantity));
        Assert.Equal(first.StockQuantity - 1, (await Toy(MagneticSet)).StockQuantity);
        Assert.Equal(second.StockQuantity - 1, (await Toy(Explorer)).StockQuantity);
    }

    [Fact]
    public async Task TC14_Stock_drops_below_cart_quantity_before_payment()
    {
        await Login();
        await AddToCart(Explorer);
        await using var db = OpenDatabase();
        await db.Products.Where(p => p.Title == Explorer)
            .ExecuteUpdateAsync(set => set.SetProperty(p => p.StockQuantity, 0));

        await PlaceOrder();
        await Expect(page.Locator(".validation-summary-errors")).ToContainTextAsync("no longer available in the requested quantity");
        Assert.False(await db.Orders.AnyAsync());
        Assert.Equal(0, (await Toy(Explorer)).StockQuantity);
        Assert.Equal(1, await db.CartItems.SumAsync(i => i.Quantity));
    }

    [Fact]
    public async Task TC15_Active_ten_percent_coupon_applies_to_cart_checkout_and_order()
    {
        await Login();
        await AddToCart(Explorer);
        await page.GotoAsync("/Cart");
        await page.Locator("#couponCode").SelectOptionAsync("B");
        await Button("Apply selection").ClickAsync();
        // $32.00 plus $3.20 tax, less $3.52 for coupon B.
        await Expect(page.Locator("dd.total")).ToHaveTextAsync("$31.68");
        await page.GotoAsync("/Orders/Checkout");
        await Expect(page.Locator("dd.total")).ToHaveTextAsync("$31.68");
        await page.Locator("#ShippingAddress").FillAsync("12 Test Street");
        await Button("Place simulated order").ClickAsync();
        await Expect(page.Locator("dd.total")).ToHaveTextAsync("$31.68");
        await using var db = OpenDatabase();
        var order = await db.Orders.SingleAsync();
        Assert.Equal("B", order.CouponCode);
        Assert.Equal(3.52m, order.DiscountAmount);
        Assert.Equal(31.68m, order.Total);
        Assert.Equal(99998, (await db.CustomerCoupons.SingleAsync(c => c.Code == "B")).Quantity);
    }

    [Fact]
    public async Task TC17_Clearing_cart_before_payment_preserves_stock()
    {
        await Login();
        var before = await Toy(Explorer);
        await AddToCart(Explorer);
        await page.GotoAsync("/Cart");
        await Button("Clear cart").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Your cart is empty" })).ToBeVisibleAsync();
        await using var db = OpenDatabase();
        Assert.False(await db.CartItems.AnyAsync());
        Assert.False(await db.Orders.AnyAsync());
        Assert.Equal(before.StockQuantity, (await Toy(Explorer)).StockQuantity);
    }
}

public class UnimplementedWorkflows
{
    [Fact(Skip = "UC11 has no supplier or purchase-order entry screen. Verify a two-line PO is saved as Ordered with the sum of quantity × unit cost when implemented.")]
    public void TC10_Create_supplier_purchase_order() { }

    [Fact(Skip = "UC13 specifies cash tender >= total. The current payment screen accepts simulated payments. TC13_Simulated_checkout covers the receipt, order and stock.")]
    public void TC13_Accept_sufficient_cash_tender() { }
}
