using ChildrensToyShop.Web.Data;
using ChildrensToyShop.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChildrensToyShop.Web.Controllers;

[Authorize]
public class ReviewsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        IQueryable<Review> reviews = db.Reviews.Include(x => x.Product).Include(x => x.User);
        if (!User.IsAdmin()) reviews = reviews.Where(x => x.UserId == User.CurrentUserId());
        return View(await reviews.OrderByDescending(x => x.UpdatedAt).ToListAsync());
    }

    public async Task<IActionResult> Create(int productId)
    {
        var product = await db.Products.FindAsync(productId); if (product is null) return NotFound();
        if (await db.Reviews.AnyAsync(x => x.ProductId == productId && x.UserId == User.CurrentUserId())) { TempData["Error"] = "You have already reviewed this toy."; return RedirectToAction("Details", "Products", new { id = productId }); }
        ViewBag.ProductTitle = product.Title; return View(new Review { ProductId = productId, ActualAgeSuitability = product.AgeRange });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Review review)
    {
        var product = await db.Products.FindAsync(review.ProductId); if (product is null) return NotFound();
        if (await db.Reviews.AnyAsync(x => x.ProductId == review.ProductId && x.UserId == User.CurrentUserId())) ModelState.AddModelError(string.Empty, "You have already reviewed this toy.");
        if (!ModelState.IsValid) { ViewBag.ProductTitle = product.Title; return View(review); }
        review.UserId = User.CurrentUserId(); review.VisibilityStatus = ReviewStatus.Pending; db.Reviews.Add(review); await db.SaveChangesAsync(); TempData["Success"] = "Review submitted for moderation."; return RedirectToAction("Details", "Products", new { id = review.ProductId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var review = await db.Reviews.Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == id); if (review is null || (!User.IsAdmin() && review.UserId != User.CurrentUserId())) return NotFound();
        ViewBag.ProductTitle = review.Product?.Title; return View(review);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Review review)
    {
        var stored = await db.Reviews.FindAsync(review.Id); if (stored is null || (!User.IsAdmin() && stored.UserId != User.CurrentUserId())) return NotFound();
        if (!ModelState.IsValid) { ViewBag.ProductTitle = (await db.Products.FindAsync(review.ProductId))?.Title; return View(review); }
        stored.Rating = review.Rating; stored.Comment = review.Comment; stored.ActualAgeSuitability = review.ActualAgeSuitability; stored.UpdatedAt = DateTime.UtcNow;
        if (User.IsAdmin()) stored.VisibilityStatus = review.VisibilityStatus; else stored.VisibilityStatus = ReviewStatus.Pending;
        await db.SaveChangesAsync(); TempData["Success"] = "Review updated."; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await db.Reviews.FindAsync(id); if (review is null || (!User.IsAdmin() && review.UserId != User.CurrentUserId())) return NotFound();
        db.Reviews.Remove(review); await db.SaveChangesAsync(); TempData["Success"] = "Review deleted."; return RedirectToAction(nameof(Index));
    }
}
