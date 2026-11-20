using ChildrensToyShop.Web.Data;
using ChildrensToyShop.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace ChildrensToyShop.Tests;

// xUnit creates a new instance for each test, so cookies, carts and stock are isolated.
public sealed partial class ShopSystemTests : IAsyncLifetime
{
    private readonly string databaseName = $"ToyShopTests_{Guid.NewGuid():N}";
    private string connectionString = null!;
    private bool ownsDatabase;
    private WebApplicationFactory<Product>? factory;
    private IPlaywright? playwright;
    private IBrowser? browser;
    private IPage page = null!;
    private const string MagneticSet = "90 Piece Magnetic Building Set";
    private const string Explorer = "Junior Explorer Kit";

    public async Task InitializeAsync()
    {
        var settings = new SqlConnectionStringBuilder
        {
            DataSource = Environment.GetEnvironmentVariable("TOYSHOP_TEST_SERVER") ?? @"localhost\SQLEXPRESS",
            InitialCatalog = "master", IntegratedSecurity = true, TrustServerCertificate = true,
            Pooling = false, ConnectTimeout = 10
        };
        await using (var master = new SqlConnection(settings.ConnectionString))
        {
            await master.OpenAsync();
            using var create = master.CreateCommand();
            create.CommandText = $"CREATE DATABASE [{databaseName}]";
            await create.ExecuteNonQueryAsync();
            ownsDatabase = true;
        }
        settings.InitialCatalog = databaseName;
        connectionString = settings.ConnectionString;
        try
        {
            factory = new WebApplicationFactory<Product>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString));
                });
            });
            factory.UseKestrel(0);
            using var client = factory.CreateClient();
            await using var db = OpenDatabase();
            var customer = await db.Users.SingleAsync(x => x.Email == "customer@toyshop.local");
            foreach (var code in new[] { "A", "B", "C" })
                db.CustomerCoupons.Add(new CustomerCoupon { UserId = customer.Id, Code = code, Quantity = 99999 });
            await db.SaveChangesAsync();

            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new() { Channel = "msedge", Headless = true });
            page = await browser.NewPageAsync(new() { BaseURL = client.BaseAddress!.ToString(), Locale = "en-US" });
            page.SetDefaultTimeout(10000);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (browser != null) { await browser.CloseAsync(); browser = null; }
        playwright?.Dispose();
        playwright = null;
        if (factory != null) { await factory.DisposeAsync(); factory = null; }
        if (ownsDatabase)
        {
            await using var db = OpenDatabase();
            await db.Database.EnsureDeletedAsync();
            ownsDatabase = false;
        }
    }

    private ApplicationDbContext OpenDatabase() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options);

    private ILocator Button(string name) => page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private async Task Login(string account = "customer", string password = "123456", IPage? session = null)
    {
        var loginPage = session ?? page;
        await loginPage.GotoAsync("/Auth/Login");
        await loginPage.Locator("#Email").FillAsync($"{account}@toyshop.local");
        await loginPage.Locator("#Password").FillAsync(password);
        await loginPage.GetByRole(AriaRole.Button, new() { Name = "Sign in", Exact = true }).ClickAsync();
    }

    private async Task<Product> Toy(string title)
    {
        await using var db = OpenDatabase();
        return await db.Products.AsNoTracking().SingleAsync(x => x.Title == title);
    }

    private async Task AddToCart(string title)
    {
        var toy = await Toy(title);
        await page.GotoAsync($"/Products/Details/{toy.Id}");
        await Button("Add to cart").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("added to your cart");
    }

    private async Task PlaceOrder()
    {
        await page.GotoAsync("/Orders/Checkout");
        await page.Locator("#ShippingAddress").FillAsync("12 Test Street");
        await Button("Place simulated order").ClickAsync();
    }

    private async Task FillNewToy(string sku, string price = "19.95")
    {
        await page.GotoAsync("/Products/Create");
        foreach (var (id, value) in new Dictionary<string, string>
        {
            ["Sku"] = sku, ["Title"] = "System Test Train", ["Description"] = "Wooden train",
            ["Price"] = price, ["AgeRange"] = "3-5 years", ["Category"] = "Vehicles",
            ["Brand"] = "Test Toys", ["StockQuantity"] = "5", ["ReorderThreshold"] = "2"
        })
            await page.Locator("#" + id).FillAsync(value);
        await Button("Save toy").ClickAsync();
    }
}
