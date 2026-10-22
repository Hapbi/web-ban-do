using System.Security.Claims;
using CuaHangGiaDung.Controllers;
using CuaHangGiaDung.Data;
using CuaHangGiaDung.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Tìm thư mục dự án (nơi có wwwroot), kể cả khi chạy file .exe trong bin/Debug
var projectDir = new DirectoryInfo(AppContext.BaseDirectory);
while (projectDir != null && !Directory.Exists(Path.Combine(projectDir.FullName, "wwwroot")))
    projectDir = projectDir.Parent;
var contentRoot = projectDir?.FullName ?? Directory.GetCurrentDirectory();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = contentRoot });
if (string.IsNullOrEmpty(builder.Configuration["urls"])) builder.WebHost.UseUrls("http://localhost:5090");

builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddDbContext<AppDb>(o => o.UseSqlite("Data Source=" + Path.Combine(contentRoot, "shop.db")));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddAuthentication("Cookies").AddCookie("Cookies", o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/Denied";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.ExpireTimeSpan = TimeSpan.FromDays(7);
    // Admin đổi vai trò/quyền chức năng thì áp dụng ngay, xóa user thì phiên đăng nhập mất hiệu lực
    o.Events.OnValidatePrincipal = async c =>
    {
        var db = c.HttpContext.RequestServices.GetRequiredService<AppDb>();
        int.TryParse(c.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        var u = await db.Users.FindAsync(id);
        if (u == null || u.IsLocked) // xóa hoặc khóa tài khoản thì phiên đăng nhập mất hiệu lực
        {
            c.RejectPrincipal();
            await c.HttpContext.SignOutAsync("Cookies");
            return;
        }
        var have = c.Principal.FindAll(CuaHangGiaDung.Helpers.Ext.PermClaim).Select(x => x.Value).OrderBy(x => x);
        var want = Perm.Effective(u.Role, u.Permissions).OrderBy(x => x);
        if (u.Role != c.Principal.FindFirstValue(ClaimTypes.Role) || !have.SequenceEqual(want))
        {
            c.ReplacePrincipal(AccountController.MakePrincipal(u));
            c.ShouldRenew = true;
        }
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();
    // Nâng cấp database cũ mà không mất dữ liệu (database mới tự bỏ qua)
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN Permissions TEXT NOT NULL DEFAULT ''"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Orders ADD COLUMN PaymentMethod TEXT NOT NULL DEFAULT 'QR'"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Products ADD COLUMN IsHidden INTEGER NOT NULL DEFAULT 0"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN IsLocked INTEGER NOT NULL DEFAULT 0"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Products ADD COLUMN Brand TEXT"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Products ADD COLUMN Tags TEXT"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Products ADD COLUMN DetailedConfiguration TEXT"); } catch { }
    db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ProductReviews (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        ProductId INTEGER NOT NULL,
        CustomerName TEXT NOT NULL DEFAULT '',
        Rating INTEGER NOT NULL DEFAULT 5,
        Comment TEXT NOT NULL DEFAULT '',
        Created TEXT NOT NULL DEFAULT ''
    )");
    db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Posts (Id INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT NOT NULL DEFAULT '', Summary TEXT, Content TEXT, ImageUrl TEXT, IsPublished INTEGER NOT NULL DEFAULT 0, Created TEXT NOT NULL DEFAULT '', Author TEXT)");
    db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Contacts (Id INTEGER PRIMARY KEY AUTOINCREMENT, Type TEXT, Name TEXT, Email TEXT, Phone TEXT, Message TEXT, Status TEXT, StaffNote TEXT, HandledBy TEXT, Created TEXT NOT NULL DEFAULT '')");
    db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS ProductCategories (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT)");
    db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS ProductBrands (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT)");
    db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS ProductTags (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT)");
    db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Banners (Id INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT, ImageUrl TEXT, LinkUrl TEXT, SortOrder INTEGER NOT NULL DEFAULT 0, IsActive INTEGER NOT NULL DEFAULT 1)");
    db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS AuditLogs (Id INTEGER PRIMARY KEY AUTOINCREMENT, Time TEXT NOT NULL DEFAULT '', UserEmail TEXT, Action TEXT, Detail TEXT)");
    // Chỉ còn 3 vai trò Admin / Staff / Customer: bỏ vai trò Employee cũ, đổi tên hiển thị Staff
    db.Database.ExecuteSqlRaw("DELETE FROM Users WHERE Role = 'Employee' AND Email = 'nhanvien@shop.vn'");
    db.Database.ExecuteSqlRaw("UPDATE Users SET Role = 'Staff' WHERE Role = 'Employee'");
    db.Database.ExecuteSqlRaw("UPDATE Users SET FullName = 'Staff' WHERE Role = 'Staff' AND FullName = 'Nhân viên'");
    db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Favorites (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER NOT NULL, ProductId INTEGER NOT NULL)");
    if (db.Users.Any() && !db.Users.Any(u => u.Role == "Admin"))
    {
        var a = db.Users.FirstOrDefault(u => u.Email == "admin@shop.vn") ?? new AppUser { Email = "admin@shop.vn", FullName = "Quản trị viên" };
        a.Role = "Admin";
        a.Permissions = "";
        a.PasswordHash = Pw.Hash(a, "Admin@123");
        if (a.Id == 0) db.Users.Add(a);
        db.SaveChanges();
    }
    if (!db.Users.Any())
    {
        var admin = new AppUser { Email = "admin@shop.vn", FullName = "Quản trị viên", Role = "Admin" };
        admin.PasswordHash = Pw.Hash(admin, "Admin@123");
        var staff = new AppUser { Email = "staff@shop.vn", FullName = "Staff", Role = "Staff" };
        staff.PasswordHash = Pw.Hash(staff, "Staff@123");
        db.Users.AddRange(admin, staff);
        db.Products.AddRange(
            new Product { Name = "Máy xay pin 21V ProMax", Category = "Điện cầm tay", Price = 1290000, Stock = 30, ImageUrl = "https://picsum.photos/seed/g1/400/300", Description = "21V • 2 pin Li-ion • 1.800 vòng/phút\nLưỡi inox • Bảo hành 12 tháng" },
            new Product { Name = "Bộ dụng cụ nhà bếp 108 món", Category = "Đồ gia dụng", Price = 890000, Stock = 40, ImageUrl = "https://picsum.photos/seed/g2/400/300", Description = "Bộ 108 món đầy đủ cho gian bếp.\nChất liệu bền, dễ vệ sinh." },
            new Product { Name = "Máy sấy tóc 1.800W", Category = "Đồ gia dụng", Price = 749000, Stock = 35, ImageUrl = "https://picsum.photos/seed/g3/400/300", Description = "Công suất 1.800W, 3 mức nhiệt.\nBảo hành 12 tháng." },
            new Product { Name = "Nồi áp suất inox 5L", Category = "Đồ gia dụng", Price = 599000, Stock = 25, ImageUrl = "https://picsum.photos/seed/g4/400/300", Description = "Dung tích 5L, inox bền chắc.\nVan an toàn nhiều lớp." },
            new Product { Name = "Nồi chiên không dầu 5L", Category = "Đồ gia dụng", Price = 2190000, Stock = 20, ImageUrl = "https://picsum.photos/seed/g5/400/300", Description = "Dung tích 5L • Chính hãng\nNhiều chế độ nấu." },
            new Product { Name = "Robot hút bụi lau nhà", Category = "Đồ gia dụng", Price = 6490000, Stock = 10, ImageUrl = "https://picsum.photos/seed/g6/400/300", Description = "Màu trắng • Bảo hành 24 tháng\nHút bụi và lau nhà tự động." });
        db.Settings.Add(new ShopSetting { ShopName = "Gia Dụng Tốt", Address = "123 Đường ABC, Hà Nội", Phone = "0123 456 789", Email = "lienhe@giadungtot.vn", OpenHours = "8:00 - 21:00 hằng ngày", BankName = "Vietcombank", AccountNo = "0123456789", AccountHolder = "NGUYEN VAN A" });
        db.SaveChanges();
    }

    // Chuẩn hóa ảnh cũ: dùng ảnh local đáng tin cậy thay cho URL picsum/URL cũ dễ lỗi.
    foreach (var product in db.Products.ToList())
    {
        var name = (product.Name ?? "").ToLowerInvariant();
        var category = (product.Category ?? "").ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(product.ImageUrl) || product.ImageUrl.Contains("picsum.photos", StringComparison.OrdinalIgnoreCase))
        {
            product.ImageUrl = name.Contains("robot") || name.Contains("hút bụi")
                ? "/images/products/robot-vacuum.svg"
                : name.Contains("sấy tóc")
                ? "/images/products/hair-dryer.svg"
                : name.Contains("trái cây") || name.Contains("ép") || name.Contains("xay")
                ? "/images/products/fruit-juicer.svg"
                : category.Contains("điện cầm tay") || name.Contains("khoan") || name.Contains("dụng cụ") || name.Contains("tua vít") || name.Contains("kìm") || name.Contains("búa")
                ? "/images/products/power-tool.svg"
                : name.Contains("nồi") || name.Contains("bếp")
                ? "/images/products/kitchen-appliance.svg"
                : "/images/products/product-default.svg";
        }
    }
    db.SaveChanges();

    // Danh mục / thương hiệu / tag mẫu (chỉ tạo khi bảng còn trống)
    if (!db.ProductCategories.Any())
    {
        db.ProductCategories.AddRange(db.Products.Select(p => p.Category).Where(c => c != null && c != "").Distinct().ToList().Select(c => new ProductCategory { Name = c }));
        db.SaveChanges();
    }
    if (!db.ProductBrands.Any())
    {
        db.ProductBrands.AddRange(new ProductBrand { Name = "Sunhouse" }, new ProductBrand { Name = "Philips" }, new ProductBrand { Name = "Kangaroo" });
        db.SaveChanges();
    }
    if (!db.ProductTags.Any())
    {
        db.ProductTags.AddRange(new ProductTag { Name = "Bán chạy" }, new ProductTag { Name = "Mới" }, new ProductTag { Name = "Khuyến mãi" });
        db.SaveChanges();
    }

    // Chạy "dotnet run -- --reset-admin" để đặt lại tài khoản admin về mặc định
    if (args.Contains("--reset-admin"))
    {
        var adm = db.Users.FirstOrDefault(u => u.Email == "admin@shop.vn") ?? new AppUser { Email = "admin@shop.vn", FullName = "Quản trị viên" };
        adm.Role = "Admin";
        adm.Permissions = "";
        adm.PasswordHash = Pw.Hash(adm, "Admin@123");
        if (adm.Id == 0) db.Users.Add(adm);
        db.SaveChanges();
        Console.WriteLine(">>> Da dat lai tai khoan: admin@shop.vn / Admin@123");
    }
}

app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
