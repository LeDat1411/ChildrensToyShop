using ChildrensToyShop.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChildrensToyShop.Web.ViewComponents;

public class CouponBalanceViewComponent(ApplicationDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = HttpContext.User.CurrentUserId();
        var balances = await db.CustomerCoupons
            .Where(coupon => coupon.UserId == userId)
            .ToDictionaryAsync(coupon => coupon.Code, coupon => coupon.Quantity);
        return View(balances);
    }
}
