using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CuaHangGiaDung.Models;

public class AppUser
{
    public int Id { get; set; }
    [Required] public string Email { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; } = "Customer"; // Admin | Staff | Customer
    public string Permissions { get; set; } = ""; // quyền chức năng của Staff, xem Perm (rỗng = mặc định theo vai trò)
    public bool IsLocked { get; set; } // tài khoản bị khóa không đăng nhập được
}

public class Product
{
    public int Id { get; set; }
    [Required] public string Name { get; set; }
    public string Category { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string ImageUrl { get; set; }
    public string Description { get; set; }
    public bool IsHidden { get; set; } // true = đã ẩn, khách không thấy
    public string Brand { get; set; }
    public string Tags { get; set; } // các tag cách nhau bằng dấu phẩy
    public string DetailedConfiguration { get; set; } // cấu hình kỹ thuật chi tiết, mỗi dòng một thông số
    [NotMapped] public double AverageRating { get; set; }
    [NotMapped] public int ReviewCount { get; set; }
}

public class ProductReview
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string CustomerName { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
    public string Receiver { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = Sts.All[0];
    public string PaymentMethod { get; set; } = PayM.QR; // QR | COD
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int Qty { get; set; }
}

public class ShopSetting
{
    public int Id { get; set; }
    public string ShopName { get; set; }
    public string Address { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string OpenHours { get; set; }
    public string BankName { get; set; }
    public string AccountNo { get; set; }
    public string AccountHolder { get; set; }
    public string QrImage { get; set; }
}

public static class PayM
{
    public const string QR = "QR";
    public const string COD = "COD";
    public static string Label(string m) => m == COD ? "Thanh toán khi nhận hàng (COD)" : "Chuyển khoản QR";
}

public static class Sts
{
    // Đơn COD không dùng "Chờ thanh toán" và "Đã thanh toán" (tiền thu khi giao hàng)
    public static string[] For(string method) => method == PayM.COD ? All.Where((s, i) => i != 0 && i != 2).ToArray() : All;
    // Đơn tính vào doanh thu: chuyển khoản = khi shop đã xác nhận tiền; COD = chỉ khi đã giao xong và thu tiền
    public static bool Counts(Order o) => o.PaymentMethod == PayM.COD ? o.Status == "Hoàn thành" : Paid.Contains(o.Status);
    public static readonly string[] All = { "Chờ thanh toán", "Chờ xác nhận", "Đã thanh toán", "Đang giao", "Hoàn thành", "Đã hủy" };
    // Chỉ các đơn đã được nhân viên xác nhận tiền mới tính vào doanh thu
    public static readonly string[] Paid = { "Đã thanh toán", "Đang giao", "Hoàn thành" };
}

public static class Pw
{
    static readonly PasswordHasher<AppUser> hasher = new();
    public static string Hash(AppUser u, string p) => hasher.HashPassword(u, p);
    public static bool Check(AppUser u, string p) => hasher.VerifyHashedPassword(u, u.PasswordHash, p) != PasswordVerificationResult.Failed;
}

// Phân quyền theo từng tài khoản.
// - Admin luôn có mọi quyền (không chỉnh được).
// - Staff: quyền lưu ở AppUser.Permissions (chuỗi, cách nhau bằng dấu phẩy).
//     ""     = chưa cấu hình -> dùng bộ quyền mặc định (Perm.Staff), tương thích database cũ
//     "-"    = Admin đã chủ động tắt hết quyền
//     "a,b"  = chỉ các quyền được liệt kê
// - Customer: không có quyền quản trị nào.
public record PermInfo(string Key, string Label, string Group);

public static class Perm
{
    public const string None = "-";

    // Danh mục chức năng Admin có thể cấp / thu hồi cho từng tài khoản (Staff).
    // Quản lý tài khoản & phân quyền KHÔNG nằm trong danh sách: chỉ Admin, để tránh tự nâng quyền.
    public static readonly PermInfo[] All =
    {
        new("revenue.view",    "Xem doanh thu trên Dashboard",        "Dashboard"),
        new("stock.view",      "Xem tồn kho trên Dashboard",          "Dashboard"),
        new("product.manage",  "Xem / thêm / sửa / ẩn sản phẩm",      "Sản phẩm"),
        new("price.set",       "Đổi giá sản phẩm",                    "Sản phẩm"),
        new("product.delete",  "Xóa vĩnh viễn sản phẩm",              "Sản phẩm"),
        new("taxonomy.manage", "Danh mục · Thương hiệu · Tag",        "Sản phẩm"),
        new("order.view",      "Xem & in đơn hàng",                   "Bán hàng"),
        new("order.status",    "Đổi trạng thái / hủy đơn hàng",       "Bán hàng"),
        new("post.manage",     "Quản lý bài viết",                    "Nội dung & khách hàng"),
        new("contact.manage",  "Xử lý liên hệ & phản ánh",            "Nội dung & khách hàng"),
        new("banner.manage",   "Quản lý banner trang chủ",            "Nội dung & khách hàng"),
        new("report.view",     "Xem báo cáo vận hành",                "Báo cáo & hệ thống"),
        new("stats.view",      "Xem thống kê & xuất CSV doanh thu",   "Báo cáo & hệ thống"),
        new("settings.manage", "Cấu hình website / QR thanh toán",    "Báo cáo & hệ thống"),
        new("log.view",        "Xem nhật ký hệ thống",                "Báo cáo & hệ thống"),
    };

    public static readonly string[] Keys = All.Select(x => x.Key).ToArray();

    // Bộ quyền mặc định của Staff (giữ nguyên hành vi trước khi có tính năng phân quyền)
    public static readonly string[] Staff =
    {
        "product.manage", "order.view", "order.status", "revenue.view", "stock.view",
        "post.manage", "contact.manage", "report.view"
    };
    public static readonly string[] Backoffice = { "Admin", "Staff" };

    public static string[] Parse(string stored) =>
        (stored ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(Keys.Contains).Distinct().ToArray();

    // Danh sách quyền thực tế của một tài khoản
    public static string[] Effective(string role, string stored)
    {
        if (role == "Admin") return Keys;
        if (role != "Staff") return Array.Empty<string>();
        var s = (stored ?? "").Trim();
        if (s == "") return Staff;
        if (s == None) return Array.Empty<string>();
        return Parse(s);
    }

    // Chuỗi lưu vào database từ các quyền được tick
    public static string Serialize(IEnumerable<string> keys)
    {
        var list = Keys.Where(k => (keys ?? Array.Empty<string>()).Contains(k)).ToArray(); // chỉ giữ khóa hợp lệ, đúng thứ tự
        return list.Length == 0 ? None : string.Join(",", list);
    }
}

public class Favorite
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
}

public record CatInfo(string Name, int Count);
public class CartLine { public Product P { get; set; } public int Qty { get; set; } }
public class ProductsVm
{
    public List<Product> Items { get; set; }
    public List<string> Cats { get; set; }
    public string Q { get; set; }
    public string[] SelCats { get; set; }
    public string[] SelPrices { get; set; }
}

public class DashVm
{
    public decimal Today, Month, Total;
    public int PaidCount, WaitConfirm, WaitPay;
    public decimal WaitConfirmAmount;
    public List<(string Label, decimal Amount)> Days = new();
    public List<(string Name, int Qty, decimal Amount)> Top = new();
    public List<Product> Products = new();
    public int InStock, LowStock, OutStock, HiddenCount;
    public string Filter;
}

// ----- US14: bài viết, liên hệ & phản ánh -----
public class Post
{
    public int Id { get; set; }
    [Required] public string Title { get; set; }
    public string Summary { get; set; }
    public string Content { get; set; }
    public string ImageUrl { get; set; }
    public bool IsPublished { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
    public string Author { get; set; }
}

public static class ContactSts
{
    public static readonly string[] All = { "Mới", "Đang xử lý", "Đã xử lý" };
    public static readonly string[] Types = { "Liên hệ", "Phản ánh" };
}

public class Contact
{
    public int Id { get; set; }
    public string Type { get; set; } = "Liên hệ"; // Liên hệ | Phản ánh
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Message { get; set; }
    public string Status { get; set; } = "Mới";
    public string StaffNote { get; set; }
    public string HandledBy { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
}

// ----- US15: danh mục, thương hiệu, tag -----
public class ProductCategory { public int Id { get; set; } public string Name { get; set; } }
public class ProductBrand { public int Id { get; set; } public string Name { get; set; } }
public class ProductTag { public int Id { get; set; } public string Name { get; set; } }
public class TaxRow { public int Id { get; set; } public string Name { get; set; } public int Used { get; set; } }

// ----- US18: banner + audit log -----
public class Banner
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string ImageUrl { get; set; }
    public string LinkUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AuditLog
{
    public int Id { get; set; }
    public DateTime Time { get; set; } = DateTime.Now;
    public string UserEmail { get; set; }
    public string Action { get; set; }
    public string Detail { get; set; }
}

// ----- US17: thống kê của Admin -----
public class StatsVm
{
    public DateTime From, To;
    public decimal Revenue;
    public int Orders, Cancelled;
    public List<(DateTime Day, int Orders, decimal Amount)> Days = new();
    public List<(string Name, int Qty, decimal Amount)> Top = new();
    public List<(string Category, decimal Amount)> ByCat = new();
    public List<Product> LowStock = new();
    public int InStock, Low, Out, Hidden;
}
