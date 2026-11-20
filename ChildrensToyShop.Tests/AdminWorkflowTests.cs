using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace ChildrensToyShop.Tests;

public sealed partial class ShopSystemTests
{
    [Fact]
    public async Task UC29_TC16_Archived_toy_leaves_catalogue_but_remains_in_existing_order()
    {
        await Login();
        var toy = await Toy(Explorer);
        await AddToCart(Explorer);
        await PlaceOrder();
        var orderLine = page.Locator(".order-line").Filter(new() { HasText = toy.Sku });
        await Expect(orderLine).ToContainTextAsync(Explorer);
        var orderPath = new Uri(page.Url).AbsolutePath;
        var searchPath = $"/Products?search={Uri.EscapeDataString(toy.Sku)}";

        await page.GotoAsync(searchPath);
        var toyHeading = page.GetByRole(AriaRole.Heading, new() { Name = Explorer, Exact = true });
        await Expect(toyHeading).ToBeVisibleAsync();

        await using var adminContext = await NewAdminContext();
        var admin = await adminContext.NewPageAsync();
        await Login("admin", session: admin);
        await admin.GotoAsync(searchPath);
        admin.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await admin.GetByRole(AriaRole.Button, new() { Name = "Archive", Exact = true }).ClickAsync();
        await Expect(admin.GetByRole(AriaRole.Alert)).ToContainTextAsync("Toy was archived");

        await page.ReloadAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "No toys matched those filters." })).ToBeVisibleAsync();
        await Expect(toyHeading).ToHaveCountAsync(0);
        await page.GotoAsync(orderPath);
        await Expect(orderLine).ToBeVisibleAsync();
        await Expect(orderLine).ToContainTextAsync(Explorer);
    }

    private Task<IBrowserContext> NewAdminContext() => browser!.NewContextAsync(new()
    {
        BaseURL = new Uri(page.Url).GetLeftPart(UriPartial.Authority),
        Locale = "en-US"
    });

    [Fact]
    public async Task TC18_Admin_processes_order_and_customer_sees_processing()
    {
        await Login();
        await AddToCart(Explorer);
        await PlaceOrder();
        await Expect(page.Locator(".section-heading .status")).ToHaveTextAsync("PAID");
        var orderPath = new Uri(page.Url).AbsolutePath;

        await using var adminContext = await NewAdminContext();
        var admin = await adminContext.NewPageAsync();
        await Login("admin", session: admin);
        await admin.GetByRole(AriaRole.Link, new() { Name = "Orders", Exact = true }).ClickAsync();
        await admin.Locator($"tr a[href='{orderPath}']").ClickAsync();
        await admin.GetByLabel("Status", new() { Exact = true }).SelectOptionAsync("PROCESSING");
        await admin.GetByRole(AriaRole.Button, new() { Name = "Update fulfillment" }).ClickAsync();
        await Expect(admin.Locator(".section-heading .status")).ToHaveTextAsync("PROCESSING");

        await page.GetByRole(AriaRole.Link, new() { Name = "Orders", Exact = true }).ClickAsync();
        var orderRow = page.GetByRole(AriaRole.Row).Filter(new() { Has = page.Locator($"a[href='{orderPath}']") });
        await Expect(orderRow.Locator(".status")).ToHaveTextAsync("PROCESSING");
        await orderRow.GetByRole(AriaRole.Link, new() { Name = "View" }).ClickAsync();
        await Expect(page.Locator(".section-heading .status")).ToHaveTextAsync("PROCESSING");
    }

    [Fact]
    public async Task TC19_Admin_approves_review_and_customer_sees_it_in_parent_feedback()
    {
        const string comment = "The compass was easy to use on our family walk.";
        await Login();
        var toy = await Toy(Explorer);
        var productPath = $"/Products/Details/{toy.Id}";
        await page.GotoAsync(productPath);
        await page.GetByRole(AriaRole.Link, new() { Name = "Write a review" }).ClickAsync();
        await page.Locator("#Rating").SelectOptionAsync("4");
        await page.Locator("#ActualAgeSuitability").FillAsync("8-10 years");
        await page.Locator("#Comment").FillAsync(comment);
        await Button("Submit review").ClickAsync();

        var feedback = page.Locator("section").Filter(new() { HasText = "Parent feedback" });
        await Expect(feedback).ToBeVisibleAsync();
        await Expect(feedback.GetByText(comment, new() { Exact = true })).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Link, new() { Name = "Reviews", Exact = true }).ClickAsync();
        var customerReview = page.Locator("article.review").Filter(new() { HasText = comment });
        await Expect(customerReview.Locator(".status")).ToHaveTextAsync("PENDING");

        await using var adminContext = await NewAdminContext();
        var admin = await adminContext.NewPageAsync();
        await Login("admin", session: admin);
        await admin.GetByRole(AriaRole.Link, new() { Name = "Reviews", Exact = true }).ClickAsync();
        var adminReview = admin.Locator("article.review").Filter(new() { HasText = comment });
        await adminReview.GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync();
        await admin.Locator("#VisibilityStatus").SelectOptionAsync("APPROVED");
        await admin.GetByRole(AriaRole.Button, new() { Name = "Save review" }).ClickAsync();
        await Expect(adminReview.Locator(".status")).ToHaveTextAsync("APPROVED");

        await page.ReloadAsync();
        await Expect(customerReview.Locator(".status")).ToHaveTextAsync("APPROVED");
        await page.GotoAsync(productPath);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = Explorer, Exact = true })).ToBeVisibleAsync();
        var published = feedback.Locator("article.review").Filter(new() { HasText = comment });
        await Expect(published).ToBeVisibleAsync();
        await Expect(published).ToContainTextAsync("4 / 5 stars");
        await Expect(published).ToContainTextAsync("Sample Customer");
    }
}
