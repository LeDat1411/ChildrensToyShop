using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChildrensToyShop.Web.Data;
using ChildrensToyShop.Web.Models;

namespace ChildrensToyShop.Web.Controllers;

public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await db.Products.Where(x => x.IsActive && x.Status == ProductStatus.Active).OrderByDescending(x => x.StockQuantity).Take(3).ToListAsync());
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult AccessDenied() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
