using ChildrensToyShop.Web.Data;
using ChildrensToyShop.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public IActionResult Create() => View("Edit", new Product());

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid) return View("Edit", product);
        if (await db.Products.AnyAsync(x => x.Sku == product.Sku)) { ModelState.AddModelError("Sku", "SKU already exists."); return View("Edit", product); }
        db.Products.Add(product); await db.SaveChangesAsync(); TempData["Success"] = "Toy added to the catalogue."; return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id) => await db.Products.FindAsync(id) is { } product ? View(product) : NotFound();

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Product product)
    {
        if (!ModelState.IsValid) return View(product);
        if (await db.Products.AnyAsync(x => x.Sku == product.Sku && x.Id != product.Id)) { ModelState.AddModelError("Sku", "SKU already exists."); return View(product); }
        db.Update(product); await db.SaveChangesAsync(); TempData["Success"] = "Toy details updated."; return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id); if (product is null) return NotFound();
        product.IsActive = false; product.Status = ProductStatus.Discontinued; await db.SaveChangesAsync(); TempData["Success"] = "Toy was archived; order history remains intact."; return RedirectToAction(nameof(Index));
    }
}
