using ChildrensToyShop.Web.Data;
using ChildrensToyShop.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ChildrensToyShop.Web.Controllers;

public class ProductsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? search, string? category, string? ageRange, string? brand, bool inStock = false)
    {
        var all = db.Products.Where(x => x.IsActive);
        if (!User.IsAdmin()) all = all.Where(x => x.Status == ProductStatus.Active);
        if (!string.IsNullOrWhiteSpace(search)) all = all.Where(x => x.Title.Contains(search) || x.Sku.Contains(search) || x.Description.Contains(search));
        if (!string.IsNullOrWhiteSpace(category)) all = all.Where(x => x.Category == category);
        if (!string.IsNullOrWhiteSpace(ageRange)) all = all.Where(x => x.AgeRange == ageRange);
        if (!string.IsNullOrWhiteSpace(brand)) all = all.Where(x => x.Brand == brand);
        if (inStock) all = all.Where(x => x.StockQuantity > 0);
        var catalogue = db.Products.Where(x => x.IsActive);
        return View(new ProductListViewModel
        {
            Products = await all.OrderBy(x => x.Title).ToListAsync(), Search = search, Category = category, AgeRange = ageRange, Brand = brand, InStock = inStock,
            Categories = await catalogue.Select(x => x.Category).Distinct().OrderBy(x => x).ToListAsync(),
            AgeRanges = await catalogue.Select(x => x.AgeRange).Distinct().OrderBy(x => x).ToListAsync(),
            Brands = await catalogue.Select(x => x.Brand).Distinct().OrderBy(x => x).ToListAsync()
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await db.Products.Include(x => x.Reviews).ThenInclude(x => x.User).SingleOrDefaultAsync(x => x.Id == id && (x.IsActive || User.IsAdmin()));
        if (product is null) return NotFound();
        return View(product);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> LowStock()
    {
        var products = await db.Products.AsNoTracking()
            .Where(x => x.IsActive && x.Status != ProductStatus.Discontinued && x.StockQuantity < x.ReorderThreshold)
            .OrderByDescending(x => x.ReorderThreshold - x.StockQuantity)
            .ThenBy(x => x.Title)
            .ToListAsync();

        return View(products);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View("Edit", new Product());

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        product.Id = 0;
        if (!await ValidateProductAsync(product)) return View("Edit", product);

        product.CreatedAt = DateTime.UtcNow;
        db.Products.Add(product);
        if (!await SaveProductAsync()) return View("Edit", product);

        TempData["Success"] = "Toy added to the catalogue.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id) => await db.Products.FindAsync(id) is { } product ? View(product) : NotFound();

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Product product)
    {
        var existing = await db.Products.FindAsync(product.Id);
        if (existing is null) return NotFound();
        if (!await ValidateProductAsync(product)) return View(product);

        product.CreatedAt = existing.CreatedAt;
        db.Entry(existing).CurrentValues.SetValues(product);
        if (!await SaveProductAsync()) return View(product);

        TempData["Success"] = "Toy details updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ValidateProductAsync(Product product)
    {
        product.SafetyNotes ??= string.Empty;
        product.SafetyCertification ??= string.Empty;
        if (ModelState.GetFieldValidationState(nameof(Product.Sku)) == ModelValidationState.Valid)
        {
            product.Sku = product.Sku.Trim();
            if (await db.Products.AnyAsync(x => x.Sku == product.Sku && x.Id != product.Id))
                ModelState.AddModelError(nameof(Product.Sku), "A toy with this SKU already exists.");
        }

        return ModelState.IsValid;
    }

    private async Task<bool> SaveProductAsync()
    {
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            ModelState.AddModelError(nameof(Product.Sku), "A toy with this SKU already exists.");
            return false;
        }
    }

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id); if (product is null) return NotFound();
        product.IsActive = false; product.Status = ProductStatus.Discontinued; await db.SaveChangesAsync(); TempData["Success"] = "Toy was archived; order history remains intact."; return RedirectToAction(nameof(Index));
    }
}
