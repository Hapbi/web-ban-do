using CuaHangGiaDung.Data;
using CuaHangGiaDung.Helpers;
using CuaHangGiaDung.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace CuaHangGiaDung.Controllers;

[Authorize(Roles = "Admin,Staff")]
public partial class ManageController(AppDb db, IWebHostEnvironment env) : AppController
{
    // Ghi nhật ký thao tác quản trị (xem ở Admin > Nhật ký hệ thống); lưu cùng lần SaveChanges kế tiếp
    void Audit(string action, string detail) =>
        db.AuditLogs.Add(new AuditLog { UserEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name, Action = action, Detail = detail });

    // Dữ liệu cho các ô chọn danh mục / thương hiệu / tag ở form sản phẩm
    void FillLookups()
    {
        ViewBag.Categories = db.ProductCategories.OrderBy(x => x.Name).Select(x => x.Name).ToList();
        ViewBag.Brands = db.ProductBrands.OrderBy(x => x.Name).Select(x => x.Name).ToList();
        ViewBag.TagList = db.ProductTags.OrderBy(x => x.Name).Select(x => x.Name).ToList();
    }

    // ----- Dashboard: nội dung hiển thị theo quyền của từng vai trò -----
    public IActionResult Dashboard(string f)
    {
        var vm = new DashVm { Filter = f };

        if (User.Can("revenue.view"))
        {
            var orders = db.Orders.Include(o => o.Items).ToList();
            var paid = orders.Where(o => Sts.Counts(o)).ToList();
            var today = DateTime.Today;
            vm.Today = paid.Where(o => o.Created.Date == today).Sum(o => o.Total);
            vm.Month = paid.Where(o => o.Created.Year == today.Year && o.Created.Month == today.Month).Sum(o => o.Total);
            vm.Total = paid.Sum(o => o.Total);
            vm.PaidCount = paid.Count;
            vm.WaitPay = orders.Count(o => o.Status == Sts.All[0]);
            var wait = orders.Where(o => o.Status == Sts.All[1]).ToList();
            vm.WaitConfirm = wait.Count;
            vm.WaitConfirmAmount = wait.Sum(o => o.Total);
            for (var d = today.AddDays(-6); d <= today; d = d.AddDays(1))
                vm.Days.Add((d.ToString("dd/MM"), paid.Where(o => o.Created.Date == d).Sum(o => o.Total)));
            vm.Top = paid.SelectMany(o => o.Items).GroupBy(i => i.ProductName)
                .Select(g => (g.Key, g.Sum(i => i.Qty), g.Sum(i => i.Price * i.Qty)))
                .OrderByDescending(x => x.Item2).Take(5).ToList();
        }

        if (User.Can("stock.view"))
        {
            var all = db.Products.ToList();
            vm.InStock = all.Count(p => p.Stock > 5);
            vm.LowStock = all.Count(p => p.Stock > 0 && p.Stock <= 5);
            vm.OutStock = all.Count(p => p.Stock <= 0);
            vm.HiddenCount = all.Count(p => p.IsHidden);
            vm.Products = (f switch
            {
                "out" => all.Where(p => p.Stock <= 0),
                "low" => all.Where(p => p.Stock > 0 && p.Stock <= 5),
                "in" => all.Where(p => p.Stock > 5),
                _ => all
            }).OrderBy(p => p.Stock).ThenBy(p => p.Name).ToList();
        }
        return View(vm);
    }

    // ----- Sản phẩm (chỉ Admin): thêm, sửa, đổi giá, ẩn, khôi phục -----
    public IActionResult Products()
    {
        if (!User.CanProducts()) return Forbid();
        return View(db.Products.OrderByDescending(p => p.Id).ToList());
    }

    public IActionResult ProductForm(int? id)
    {
        if (!User.CanProducts()) return Forbid();
        var p = id == null ? new Product() : db.Products.Find(id);
        if (p == null) return NotFound();
        FillLookups();
        return View(p);
    }

    [HttpPost]
    public async Task<IActionResult> ProductForm(Product p, string[] tagList, IFormFile? imageFile, bool removeImage = false)
    {
        if (!User.CanProducts()) return Forbid();
        var img = (p.ImageUrl ?? "").Trim();
        if (img != "" && !img.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !img.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            && !img.StartsWith("/"))
            ModelState.AddModelError("ImageUrl", "Link ảnh phải bắt đầu bằng http://, https:// hoặc /.");

        if (imageFile != null && imageFile.Length > 0)
        {
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (!allowed.Contains(ext) || !imageFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError("imageFile", "Chỉ nhận ảnh JPG, PNG, WEBP hoặc GIF.");
            if (imageFile.Length > 5 * 1024 * 1024)
                ModelState.AddModelError("imageFile", "Ảnh phải nhỏ hơn 5 MB.");
        }
        if (p.Price < 0 || p.Stock < 0) ModelState.AddModelError("", "Giá và tồn kho không được âm");
        if (!ModelState.IsValid) { FillLookups(); return View(p); }

        Product t;
        if (p.Id == 0) { t = new Product(); db.Products.Add(t); }
        else { t = db.Products.Find(p.Id); if (t == null) return NotFound(); }

        // Không gửi file ảnh vào database: lưu file trong wwwroot/uploads/products,
        // chỉ lưu đường dẫn công khai vào cột ImageUrl.
        if (imageFile != null && imageFile.Length > 0)
        {
            var uploadDir = Path.Combine(env.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadDir);
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            var fileName = Guid.NewGuid().ToString("N") + ext;
            var fullPath = Path.Combine(uploadDir, fileName);
            await using (var stream = System.IO.File.Create(fullPath))
                await imageFile.CopyToAsync(stream);
            img = "/uploads/products/" + fileName;
        }
        else if (removeImage)
        {
            img = "";
        }
        else if (p.Id != 0 && string.IsNullOrWhiteSpace(img))
        {
            // Không ghi đè ảnh cũ nếu người dùng chỉ sửa các trường khác.
            img = t.ImageUrl ?? "";
        }
        var oldImage = t.ImageUrl;
        t.Name = p.Name.Trim();
        t.Category = (p.Category ?? "").Trim();
        t.Brand = (p.Brand ?? "").Trim();
        t.Tags = string.Join(",", (tagList ?? Array.Empty<string>()).Select(x => x.Trim()).Where(x => x != "").Distinct());
        if (p.Id == 0 || User.Can("price.set")) t.Price = p.Price; // thêm mới thì đặt giá; sửa giá chỉ khi được cấp quyền price.set
        t.Stock = p.Stock;
        t.ImageUrl = img;
        if (!string.IsNullOrWhiteSpace(oldImage) && oldImage != img
            && oldImage.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase))
        {
            var oldPath = Path.Combine(env.WebRootPath, oldImage.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
        }
        t.Description = (p.Description ?? "").Trim();
        t.DetailedConfiguration = (p.DetailedConfiguration ?? "").Trim();
        Audit(p.Id == 0 ? "Thêm sản phẩm" : "Sửa sản phẩm", t.Name);
        db.SaveChanges();
        TempData["Msg"] = "Đã lưu sản phẩm";
        return RedirectToAction("Products");
    }

    [HttpPost, RequirePerm("price.set")]
    public IActionResult SetPrice(int id, string price)
    {
        var p = db.Products.Find(id);
        if (p == null) return NotFound();
        // chấp nhận cả dạng có dấu chấm (1.000.000) lẫn số thuần
        var digits = new string((price ?? "").Where(char.IsDigit).ToArray());
        if (digits == "" || !decimal.TryParse(digits, out var newPrice)) { TempData["Err"] = "Vui lòng nhập giá hợp lệ"; return RedirectToAction("Products"); }
        Audit("Đổi giá", $"{p.Name}: {p.Price.Money()} → {newPrice.Money()}");
        p.Price = newPrice;
        db.SaveChanges();
        TempData["Msg"] = $"Đã đổi giá '{p.Name}' thành {newPrice.Money()}";
        return RedirectToAction("Products");
    }

    // Xóa vĩnh viễn sản phẩm (đơn hàng cũ vẫn giữ tên + giá lúc mua)
    [HttpPost, RequirePerm("product.delete")]
    public IActionResult DeleteProduct(int id)
    {
        var p = db.Products.Find(id);
        if (p != null)
        {
            db.Favorites.RemoveRange(db.Favorites.Where(f => f.ProductId == id));
            db.Products.Remove(p);
            Audit("Xóa sản phẩm", p.Name);
            db.SaveChanges();
            TempData["Msg"] = $"Đã xóa '{p.Name}'";
        }
        return RedirectToAction("Products");
    }

    // Ẩn sản phẩm khỏi cửa hàng / khôi phục lại (không xóa dữ liệu, đơn cũ vẫn giữ nguyên)
    [HttpPost]
    public IActionResult ToggleHide(int id)
    {
        if (!User.CanProducts()) return Forbid();
        var p = db.Products.Find(id);
        if (p == null) return NotFound();
        p.IsHidden = !p.IsHidden;
        Audit(p.IsHidden ? "Ẩn sản phẩm" : "Khôi phục sản phẩm", p.Name);
        db.SaveChanges();
        TempData["Msg"] = p.IsHidden ? $"Đã ẩn '{p.Name}'" : $"Đã khôi phục '{p.Name}'";
        return RedirectToAction("Products");
    }

    // ----- Đơn hàng -----
    public IActionResult Orders()
    {
        if (!User.Can("order.view")) return Forbid();
        return View(db.Orders.Include(o => o.User).Include(o => o.Items).OrderByDescending(o => o.Id).ToList());
    }

    // In đơn hàng (trang in riêng, tự mở hộp thoại in)
    public IActionResult PrintOrder(int id)
    {
        if (!User.Can("order.view")) return Forbid();
        var o = db.Orders.Include(x => x.User).Include(x => x.Items).FirstOrDefault(x => x.Id == id);
        if (o == null) return NotFound();
        ViewBag.Shop = db.Settings.First();
        return View(o);
    }

    [HttpPost]
    public IActionResult SetStatus(int id, string status)
    {
        if (!User.Can("order.status")) return Forbid();
        var o = db.Orders.Include(x => x.Items).FirstOrDefault(x => x.Id == id);
        if (o == null) return NotFound();
        if (!Sts.All.Contains(status)) return BadRequest();
        if (o.PaymentMethod == PayM.COD && !Sts.For(PayM.COD).Contains(status))
        {
            TempData["Err"] = "Đơn COD không dùng trạng thái \"" + status + "\" (tiền thu khi giao hàng)";
            return RedirectToAction("Orders");
        }
        if (o.Status == "Đã hủy") { TempData["Err"] = "Đơn đã hủy không thể đổi trạng thái"; return RedirectToAction("Orders"); }
        if (status == "Đã hủy") // hoàn lại kho
            foreach (var i in o.Items)
            {
                var p = db.Products.Find(i.ProductId);
                if (p != null) p.Stock += i.Qty;
            }
        Audit("Đổi trạng thái đơn", $"#DH{o.Id}: {o.Status} → {status}");
        o.Status = status;
        db.SaveChanges();
        TempData["Msg"] = "Đã cập nhật trạng thái";
        return RedirectToAction("Orders");
    }

    // ----- Người dùng & phân quyền (chỉ Admin) -----
    [Authorize(Roles = "Admin")]
    public IActionResult Users() => View(db.Users.OrderBy(u => u.Id).ToList());

    [HttpPost, Authorize(Roles = "Admin")]
    public IActionResult SetRole(int id, string role)
    {
        var u = db.Users.Find(id);
        if (u == null || id == Uid || !new[] { "Admin", "Staff", "Customer" }.Contains(role)) return RedirectToAction("Users");
        Audit("Đổi vai trò", $"{u.Email}: {u.Role} → {role}");
        u.Role = role;
        u.Permissions = ""; // đổi vai trò thì quay về bộ quyền mặc định của vai trò mới
        db.SaveChanges();
        TempData["Msg"] = "Đã đổi vai trò";
        return RedirectToAction("Users");
    }

    // ----- Phân quyền chức năng cho từng tài khoản (chỉ Admin) -----
    [Authorize(Roles = "Admin")]
    public IActionResult Permissions(int id)
    {
        var u = db.Users.Find(id);
        if (u == null) return NotFound();
        if (u.Role != "Staff")
        {
            TempData["Err"] = u.Role == "Admin"
                ? "Admin luôn có toàn quyền, không cần phân quyền."
                : "Chỉ tài khoản Staff mới được cấp quyền quản trị. Hãy đổi vai trò sang Staff trước.";
            return RedirectToAction("Users");
        }
        ViewBag.Granted = Perm.Effective(u.Role, u.Permissions);
        return View(u);
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public IActionResult Permissions(int id, string[] perms, bool reset = false)
    {
        var u = db.Users.Find(id);
        if (u == null) return NotFound();
        if (u.Role != "Staff") { TempData["Err"] = "Chỉ tài khoản Staff mới được phân quyền"; return RedirectToAction("Users"); }
        u.Permissions = reset ? "" : Perm.Serialize(perms);
        var now = Perm.Effective(u.Role, u.Permissions);
        Audit("Phân quyền tài khoản", $"{u.Email}: {(now.Length == 0 ? "(không có quyền nào)" : string.Join(", ", now))}");
        db.SaveChanges(); // phiên đăng nhập của tài khoản đó tự cập nhật ở request kế tiếp
        TempData["Msg"] = reset ? $"Đã đặt {u.Email} về quyền mặc định" : $"Đã lưu quyền cho {u.Email}";
        return RedirectToAction("Users");
    }

    // ----- Cấu hình shop + mã QR (chỉ Admin) -----
    [RequirePerm("settings.manage")]
    public IActionResult Settings() => View(db.Settings.First());

    [HttpPost, RequirePerm("settings.manage")]
    public async Task<IActionResult> Settings(ShopSetting m, IFormFile qr)
    {
        var s = db.Settings.First();
        s.ShopName = m.ShopName; s.Address = m.Address; s.Phone = m.Phone; s.Email = m.Email; s.OpenHours = m.OpenHours;
        s.BankName = m.BankName; s.AccountNo = m.AccountNo; s.AccountHolder = m.AccountHolder;
        if (qr != null && qr.Length > 0)
        {
            var ext = Path.GetExtension(qr.FileName).ToLower();
            if (!new[] { ".png", ".jpg", ".jpeg", ".webp" }.Contains(ext)) { TempData["Err"] = "Ảnh QR chỉ nhận png, jpg, jpeg, webp"; return RedirectToAction("Settings"); }
            if (qr.Length > 2_000_000) { TempData["Err"] = "Ảnh QR phải nhỏ hơn 2MB"; return RedirectToAction("Settings"); }
            var dir = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(dir);
            var name = "qr_" + Guid.NewGuid().ToString("N") + ext;
            using (var f = System.IO.File.Create(Path.Combine(dir, name))) await qr.CopyToAsync(f);
            s.QrImage = name;
        }
        Audit("Sửa cấu hình website", s.ShopName);
        db.SaveChanges();
        TempData["Msg"] = "Đã lưu cấu hình";
        return RedirectToAction("Settings");
    }
}
