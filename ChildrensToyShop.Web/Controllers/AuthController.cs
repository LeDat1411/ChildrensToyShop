using System.Security.Claims;
using ChildrensToyShop.Web.Data;
using ChildrensToyShop.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChildrensToyShop.Web.Controllers;

public class AuthController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == model.Email.Trim().ToLower());
        if (user is null || user.PasswordHash != DemoData.Hash(model.Password))
        {
            ModelState.AddModelError(string.Empty, "The email address or password is incorrect.");
            return View(model);
        }
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return LocalRedirect(string.IsNullOrWhiteSpace(model.ReturnUrl) || !Url.IsLocalUrl(model.ReturnUrl) ? "/" : model.ReturnUrl);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
