using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using CuaHangGiaDung.Data;
using CuaHangGiaDung.Models;

namespace CuaHangGiaDung.Helpers;

public static class Ext
{
    // Claim lưu danh sách quyền của tài khoản (do AccountController.MakePrincipal tạo)
    public const string PermClaim = "shop:perm";

    // Admin: mọi quyền. Các tài khoản khác: chỉ những quyền Admin đã cấp (xem Perm / trang Phân quyền).
    public static bool Can(this ClaimsPrincipal u, string perm) =>
        u.IsInRole("Admin") ||
        (u.IsInRole("Staff") && u.FindAll(PermClaim).Any(c => c.Value == perm));

    // Xem / thêm / sửa / ẩn / khôi phục sản phẩm: Admin + Staff
    public static bool CanProducts(this ClaimsPrincipal u) => u.Can("product.manage");

    public static bool IsBackoffice(this ClaimsPrincipal u) => Perm.Backoffice.Any(u.IsInRole);

    public static string Money(this decimal n) => n.ToString("#,##0", CultureInfo.InvariantCulture).Replace(',', '.') + "đ";
}

public static class CartHelper
{
    public static Dictionary<int, int> Get(ISession s)
    {
        var j = s.GetString("cart");
        return j == null ? new() : JsonSerializer.Deserialize<Dictionary<int, int>>(j);
    }
    public static void Save(ISession s, Dictionary<int, int> c) => s.SetString("cart", JsonSerializer.Serialize(c));

    public static List<CartLine> Lines(ISession s, AppDb db)
    {
        var c = Get(s);
        var ids = c.Keys.ToList();
        return db.Products.Where(p => !p.IsHidden && ids.Contains(p.Id)).ToList().Select(p => new CartLine { P = p, Qty = c[p.Id] }).ToList();
    }
}
