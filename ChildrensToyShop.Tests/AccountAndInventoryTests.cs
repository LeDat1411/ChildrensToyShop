using ChildrensToyShop.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace ChildrensToyShop.Tests;

public sealed partial class ShopSystemTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData("customer")]
    public async Task UC23_TC15_Sign_out_ends_the_session(string account)
    {
        await Login(account);
        await Expect(Button("Sign out")).ToBeVisibleAsync();
        var homeUrl = new Uri(new Uri(page.Url), "/").AbsoluteUri;

        await Button("Sign out").ClickAsync();
        await Expect(page).ToHaveURLAsync(homeUrl);
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Sign in", Exact = true })).ToBeVisibleAsync();

        await page.GotoAsync("/Cart");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Sign in to your shop account" })).ToBeVisibleAsync();
    }

    [Theory, InlineData("admin", "Toy Shop Admin"), InlineData("customer", "Sample Customer")]
    public async Task TC01_Valid_login_shows_role_home(string account, string name)
    {
        await Login(account);
        Assert.Equal("/", new Uri(page.Url).AbsolutePath);
        await Expect(page.Locator("header")).ToContainTextAsync($"Hi, {name}");
        var alerts = page.GetByRole(AriaRole.Link, new() { Name = "Low Stock Alerts" });
        await Expect(alerts).ToHaveCountAsync(account == "admin" ? 1 : 0);
    }

    [Fact]
    public async Task TC02_Wrong_password_shows_error_and_leaves_cart_protected()
    {
        await Login(password: "incorrect");
        await Expect(page.Locator(".validation-summary-errors")).ToContainTextAsync("The email address or password is incorrect");
        await page.GotoAsync("/Cart");
        Assert.Equal("/Auth/Login", new Uri(page.Url).AbsolutePath);
    }

    [Fact]
    public async Task TC04_Valid_product_is_saved_and_listed()
    {
        await Login("admin");
        await FillNewToy("TEST-TRAIN");
        await Expect(page.Locator(".product-card h3").Filter(new() { HasText = "System Test Train" })).ToBeVisibleAsync();
        var toy = await Toy("System Test Train");
        Assert.Equal("TEST-TRAIN", toy.Sku);
        Assert.Equal(19.95m, toy.Price);
        Assert.Equal(5, toy.StockQuantity);
    }

    [Fact]
    public async Task TC05_Duplicate_SKU_is_rejected_and_corrected_SKU_is_saved()
    {
        await Login("admin");
        await FillNewToy("EDU-1001");
        await Expect(page.Locator("[data-valmsg-for=Sku]")).ToContainTextAsync("SKU already exists");
        await using var db = OpenDatabase();
        Assert.Equal(1, await db.Products.CountAsync(p => p.Sku == "EDU-1001"));
        Assert.False(await db.Products.AnyAsync(p => p.Title == "System Test Train"));
        await page.Locator("#Sku").FillAsync("TEST-FIXED");
        await Button("Save toy").ClickAsync();
        Assert.Equal("TEST-FIXED", (await Toy("System Test Train")).Sku);
    }

    [Fact]
    public async Task TC06_Negative_price_requires_correction_and_focuses_price()
    {
        await Login("admin");
        await FillNewToy("TEST-PRICE", "-1");
        await Expect(page.Locator("[data-valmsg-for=Price]")).ToContainTextAsync("Price must be between");
        await Expect(page.Locator("#Price")).ToBeFocusedAsync();
        await using var db = OpenDatabase();
        Assert.False(await db.Products.AnyAsync(p => p.Sku == "TEST-PRICE"));
        await page.Locator("#Price").FillAsync("2.50");
        await Button("Save toy").ClickAsync();
        Assert.Equal(2.50m, (await Toy("System Test Train")).Price);
    }

    [Fact]
    public async Task TC07_Search_magnetic_returns_matching_toy()
    {
        await page.GotoAsync("/Products");
        await page.Locator("[name=search]").FillAsync("magnetic");
        await Button("Filter").ClickAsync();
        await Expect(page.Locator(".product-card h3")).ToHaveTextAsync(new[] { MagneticSet });
    }

    [Theory, InlineData(9, true), InlineData(10, true), InlineData(11, false)]
    public async Task TC09_Alert_at_or_below_threshold(int stock, bool expectedAlert)
    {
        await using var db = OpenDatabase();
        var toy = await db.Products.SingleAsync(p => p.Title == Explorer);
        toy.ReorderThreshold = 10;
        toy.StockQuantity = stock;
        await db.SaveChangesAsync();
        await Login("admin");
        await page.GotoAsync("/Products/LowStock");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Low Stock Alerts" })).ToBeVisibleAsync();
        var listed = await page.GetByRole(AriaRole.Row).Filter(new() { HasText = toy.Sku }).CountAsync() == 1;
        Assert.Equal(expectedAlert, listed);
    }
}
