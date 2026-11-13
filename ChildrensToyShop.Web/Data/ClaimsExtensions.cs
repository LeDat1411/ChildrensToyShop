using System.Security.Claims;

namespace ChildrensToyShop.Web.Data;

public static class ClaimsExtensions
{
    public static int CurrentUserId(this ClaimsPrincipal user) => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole("Admin");
}
