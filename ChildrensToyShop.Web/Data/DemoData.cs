using System.Security.Cryptography;
using System.Text;
using ChildrensToyShop.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace ChildrensToyShop.Web.Data;

public static class DemoData
{
    public static async Task EnsureSeededAsync(ApplicationDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new AppUser { DisplayName = "Toy Shop Admin", Email = "admin@toyshop.local", PasswordHash = Hash("123456"), Role = "Admin" },
                new AppUser { DisplayName = "Sample Customer", Email = "customer@toyshop.local", PasswordHash = Hash("123456"), Role = "Customer" });
        }
        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(
                new Product { Sku = "EDU-1001", Title = "90 Piece Magnetic Building Set", Description = "Colorful magnetic tiles for imaginative construction and early STEM play.", Price = 49.99m, AgeRange = "3-5 years", Category = "Educational", Brand = "Bright Minds", SafetyCertification = "ASTM F963, CE", SafetyNotes = "Contains magnets. Adult supervision recommended.", StockQuantity = 38, ReorderThreshold = 30 },
                new Product { Sku = "ROB-2001", Title = "Robo Pup Interactive Friend", Description = "A friendly programmable puppy with sound, movement, and simple coding games.", Price = 79.99m, AgeRange = "6-8 years", Category = "Robotics", Brand = "Spark Labs", SafetyCertification = "ASTM F963", SafetyNotes = "Rechargeable battery included.", StockQuantity = 20, ReorderThreshold = 30 },
                new Product { Sku = "ART-3001", Title = "Ocean Paint Lab", Description = "Water-based paint and craft set for creative, ocean-inspired projects.", Price = 24.50m, AgeRange = "6-8 years", Category = "Arts and Crafts", Brand = "Little Palette", SafetyCertification = "CE", SafetyNotes = "Non-toxic materials. Wear a smock.", StockQuantity = 56, ReorderThreshold = 15 },
                new Product { Sku = "OUT-4001", Title = "Junior Explorer Kit", Description = "Binoculars, compass, specimen jar and activity booklet for outdoor discovery.", Price = 32.00m, AgeRange = "8-10 years", Category = "Outdoor", Brand = "Trail Tots", SafetyCertification = "ASTM F963", SafetyNotes = "Small parts; not for children under 3.", StockQuantity = 12, ReorderThreshold = 10 });
        }
        await db.SaveChangesAsync();
    }

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
