using System.ComponentModel.DataAnnotations;

namespace ChildrensToyShop.Web.Models;

public class CustomerCoupon
{
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    [StringLength(1)] public string Code { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public record CouponType(string Code, int Percent)
{
    public static IReadOnlyList<CouponType> All { get; } =
        [new("A", 5), new("B", 10), new("C", 15)];

    public static CouponType? Find(string? code) => All.FirstOrDefault(coupon => coupon.Code == code);
}
