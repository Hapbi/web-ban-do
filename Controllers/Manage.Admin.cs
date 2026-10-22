using System.Text;
using CuaHangGiaDung.Helpers;
using CuaHangGiaDung.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CuaHangGiaDung.Controllers;

// Chức năng chỉ dành cho Admin: US15 danh mục/thương hiệu/tag, US16 tài khoản nhân viên,
// US17 thống kê, US18 banner + nhật ký hệ thống
public partial class ManageController
{
    // ===== US15: danh mục, thương hiệu, tag =====
    static string NormType(string t) => t == "brand" || t == "tag" ? t : "category";
    static string[] SplitTags(string s) => (s ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    static string TypeLabel(string t) => t == "brand" ? "thương hiệu" : t == "tag" ? "tag" : "danh mục";

    List<TaxRow> TaxRows(string type)
    {
        var prods = db.Products.Select(p => new { p.Category, p.Brand, p.Tags }).ToList();
        if (type == "brand")
            return db.ProductBrands.OrderBy(x => x.Name).ToList()
                .Select(x => new TaxRow { Id = x.Id, Name = x.Name, Used = prods.Count(p => p.Brand == x.Name) }).ToList();
        if (type == "tag")
            return db.ProductTags.OrderBy(x => x.Name).ToList()
                .Select(x => new TaxRow { Id = x.Id, Name = x.Name, Used = prods.Count(p => SplitTags(p.Tags).Contains(x.Name)) }).ToList();
        return db.ProductCategories.OrderBy(x => x.Name).ToList()
            .Select(x => new TaxRow { Id = x.Id, Name = x.Name, Used = prods.Count(p => p.Category == x.Name) }).ToList();
    }

    [RequirePerm("taxonomy.manage")]
    public IActionResult Taxonomy(string type)
    {
        type = NormType(type);
        ViewBag.Type = type;
        return View(TaxRows(type));
    }

    [HttpPost, RequirePerm("taxonomy.manage")]
    public IActionResult TaxonomyAdd(string type, string name)
    {
        type = NormType(type);
        name = (name ?? "").Trim();
        if (name == "") { TempData["Err"] = "Vui lòng nhập tên"; return RedirectToAction("Taxonomy", new { type }); }
        if (TaxRows(type).Any(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
        { TempData["Err"] = $"Đã có {TypeLabel(type)} '{name}'"; return RedirectToAction("Taxonomy", new { type }); }
        if (type == "brand") db.ProductBrands.Add(new ProductBrand { Name = name });
        else if (type == "tag") db.ProductTags.Add(new ProductTag { Name = name });
        else db.ProductCategories.Add(new ProductCategory { Name = name });
        Audit("Thêm " + TypeLabel(type), name);
        db.SaveChanges();
        TempData["Msg"] = $"Đã thêm {TypeLabel(type)} '{name}'";
        return RedirectToAction("Taxonomy", new { type });
    }

    [HttpPost, RequirePerm("taxonomy.manage")]
    public IActionResult TaxonomyRename(string type, int id, string name)
    {
        type = NormType(type);
        name = (name ?? "").Trim();
        var rows = TaxRows(type);
        var row = rows.FirstOrDefault(r => r.Id == id);
        if (row == null) return NotFound();
        if (name == "") { TempData["Err"] = "Tên không được để trống"; return RedirectToAction("Taxonomy", new { type }); }
        if (rows.Any(r => r.Id != id && string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
        { TempData["Err"] = $"Đã có {TypeLabel(type)} '{name}'"; return RedirectToAction("Taxonomy", new { type }); }
        var old = row.Name;
        if (type == "brand")
        {
            db.ProductBrands.Find(id).Name = name;
            foreach (var pr in db.Products.Where(x => x.Brand == old)) pr.Brand = name;
        }
        else if (type == "tag")
        {
            db.ProductTags.Find(id).Name = name;
            foreach (var pr in db.Products.ToList().Where(x => SplitTags(x.Tags).Contains(old)))
                pr.Tags = string.Join(",", SplitTags(pr.Tags).Select(x => x == old ? name : x).Distinct());
        }
        else
        {
            db.ProductCategories.Find(id).Name = name;
            foreach (var pr in db.Products.Where(x => x.Category == old)) pr.Category = name;
        }
        Audit("Đổi tên " + TypeLabel(type), $"{old} → {name}");
        db.SaveChanges();
        TempData["Msg"] = "Đã đổi tên (sản phẩm liên quan được cập nhật theo)";
        return RedirectToAction("Taxonomy", new { type });
    }

    [HttpPost, RequirePerm("taxonomy.manage")]
    public IActionResult TaxonomyDelete(string type, int id)
    {
        type = NormType(type);
        var row = TaxRows(type).FirstOrDefault(r => r.Id == id);
        if (row == null) return NotFound();
        if (type != "tag" && row.Used > 0)
        {
            TempData["Err"] = $"Không xóa được: còn {row.Used} sản phẩm đang dùng {TypeLabel(type)} '{row.Name}'. Hãy chuyển các sản phẩm sang mục khác trước.";
            return RedirectToAction("Taxonomy", new { type });
        }
        if (type == "brand") db.ProductBrands.Remove(db.ProductBrands.Find(id));
        else if (type == "category") db.ProductCategories.Remove(db.ProductCategories.Find(id));
        else
        {
            db.ProductTags.Remove(db.ProductTags.Find(id));
            foreach (var pr in db.Products.ToList().Where(x => SplitTags(x.Tags).Contains(row.Name)))
                pr.Tags = string.Join(",", SplitTags(pr.Tags).Where(x => x != row.Name));
        }
        Audit("Xóa " + TypeLabel(type), row.Name);
        db.SaveChanges();
        TempData["Msg"] = $"Đã xóa {TypeLabel(type)} '{row.Name}'";
        return RedirectToAction("Taxonomy", new { type });
    }

    // ===== US16: tài khoản nhân viên =====
    [HttpPost, Authorize(Roles = "Admin")]
    public IActionResult CreateStaff(string email, string fullName, string password)
    {
        var e = (email ?? "").Trim().ToLower();
        if (!e.Contains('@') || string.IsNullOrWhiteSpace(fullName)) { TempData["Err"] = "Vui lòng nhập họ tên và email hợp lệ"; return RedirectToAction("Users"); }
        if ((password ?? "").Length < 6) { TempData["Err"] = "Mật khẩu tối thiểu 6 ký tự"; return RedirectToAction("Users"); }
        if (db.Users.Any(x => x.Email.ToLower() == e)) { TempData["Err"] = "Email đã tồn tại"; return RedirectToAction("Users"); }
        var u = new AppUser { Email = e, FullName = fullName.Trim(), Role = "Staff" };
        u.PasswordHash = Pw.Hash(u, password);
        db.Users.Add(u);
        Audit("Tạo tài khoản Staff", e);
        db.SaveChanges();
        TempData["Msg"] = $"Đã tạo tài khoản Staff {e}";
        return RedirectToAction("Users");
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public IActionResult ToggleLock(int id)
    {
        var u = db.Users.Find(id);
        if (u == null) return NotFound();
        if (id == Uid) { TempData["Err"] = "Không thể khóa chính tài khoản của bạn"; return RedirectToAction("Users"); }
        u.IsLocked = !u.IsLocked;
        Audit(u.IsLocked ? "Khóa tài khoản" : "Mở khóa tài khoản", u.Email);
        db.SaveChanges();
        TempData["Msg"] = u.IsLocked ? $"Đã khóa {u.Email}" : $"Đã mở khóa {u.Email}";
        return RedirectToAction("Users");
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public IActionResult ResetPassword(int id, string password)
    {
        var u = db.Users.Find(id);
        if (u == null) return NotFound();
        if ((password ?? "").Length < 6) { TempData["Err"] = "Mật khẩu mới tối thiểu 6 ký tự"; return RedirectToAction("Users"); }
        u.PasswordHash = Pw.Hash(u, password);
        Audit("Đặt lại mật khẩu", u.Email);
        db.SaveChanges();
        TempData["Msg"] = $"Đã đặt lại mật khẩu cho {u.Email}";
        return RedirectToAction("Users");
    }

    // ===== US17: thống kê tồn kho, bán chạy, doanh thu =====
    StatsVm BuildStats(DateTime? from, DateTime? to)
    {
        var end = (to ?? DateTime.Today).Date;
        var start = (from ?? end.AddDays(-29)).Date;
        if (start > end) (start, end) = (end, start);
        var vm = new StatsVm { From = start, To = end };
        var inRange = db.Orders.Include(o => o.Items).ToList().Where(o => o.Created.Date >= start && o.Created.Date <= end).ToList();
        var paid = inRange.Where(o => Sts.Counts(o)).ToList();
        vm.Revenue = paid.Sum(o => o.Total);
        vm.Orders = paid.Count;
        vm.Cancelled = inRange.Count(o => o.Status == "Đã hủy");
        for (var d = end; d >= start; d = d.AddDays(-1))
        {
            var day = paid.Where(o => o.Created.Date == d).ToList();
            vm.Days.Add((d, day.Count, day.Sum(o => o.Total)));
        }
        var items = paid.SelectMany(o => o.Items).ToList();
        vm.Top = items.GroupBy(i => i.ProductName)
            .Select(g => (g.Key, g.Sum(i => i.Qty), g.Sum(i => i.Price * i.Qty)))
            .OrderByDescending(x => x.Item2).Take(10).ToList();
        var prods = db.Products.ToList();
        var catOf = prods.ToDictionary(p => p.Id, p => string.IsNullOrEmpty(p.Category) ? "(Chưa phân loại)" : p.Category);
        vm.ByCat = items.GroupBy(i => catOf.GetValueOrDefault(i.ProductId, "(Sản phẩm đã xóa)"))
            .Select(g => (g.Key, g.Sum(i => i.Price * i.Qty))).OrderByDescending(x => x.Item2).ToList();
        vm.InStock = prods.Count(p => p.Stock > 5);
        vm.Low = prods.Count(p => p.Stock > 0 && p.Stock <= 5);
        vm.Out = prods.Count(p => p.Stock <= 0);
        vm.Hidden = prods.Count(p => p.IsHidden);
        vm.LowStock = prods.Where(p => p.Stock <= 5).OrderBy(p => p.Stock).ThenBy(p => p.Name).Take(15).ToList();
        return vm;
    }

    [RequirePerm("stats.view")]
    public IActionResult Stats(DateTime? from, DateTime? to) => View(BuildStats(from, to));

    [RequirePerm("stats.view")]
    public IActionResult StatsCsv(DateTime? from, DateTime? to)
    {
        var vm = BuildStats(from, to);
        var sb = new StringBuilder("Ngay,So don,Doanh thu\n");
        foreach (var d in vm.Days.OrderBy(r => r.Day)) sb.Append($"{d.Day:dd/MM/yyyy},{d.Orders},{(long)d.Amount}\n");
        sb.Append($"TONG,{vm.Orders},{(long)vm.Revenue}\n");
        return File(new UTF8Encoding(true).GetBytes(sb.ToString()), "text/csv", $"doanh-thu_{vm.From:yyyyMMdd}_{vm.To:yyyyMMdd}.csv");
    }

    // ===== US18: banner =====
    [RequirePerm("banner.manage")]
    public IActionResult Banners() => View(db.Banners.OrderBy(b => b.SortOrder).ThenBy(b => b.Id).ToList());

    [HttpPost, RequirePerm("banner.manage")]
    public IActionResult BannerSave(int id, string title, string imageUrl, string linkUrl, int sortOrder, bool isActive)
    {
        imageUrl = (imageUrl ?? "").Trim();
        linkUrl = (linkUrl ?? "").Trim();
        if (imageUrl == "" || (!imageUrl.StartsWith("http://") && !imageUrl.StartsWith("https://") && !imageUrl.StartsWith("/")))
        { TempData["Err"] = "Link ảnh banner phải bắt đầu bằng http://, https:// hoặc /"; return RedirectToAction("Banners"); }
        if (linkUrl != "" && !linkUrl.StartsWith("http://") && !linkUrl.StartsWith("https://") && !linkUrl.StartsWith("/"))
        { TempData["Err"] = "Link đích phải bắt đầu bằng http://, https:// hoặc /"; return RedirectToAction("Banners"); }
        Banner b;
        if (id == 0) { b = new Banner(); db.Banners.Add(b); }
        else { b = db.Banners.Find(id); if (b == null) return NotFound(); }
        b.Title = (title ?? "").Trim();
        b.ImageUrl = imageUrl;
        b.LinkUrl = linkUrl;
        b.SortOrder = sortOrder;
        b.IsActive = isActive;
        Audit(id == 0 ? "Thêm banner" : "Sửa banner", b.Title);
        db.SaveChanges();
        TempData["Msg"] = "Đã lưu banner";
        return RedirectToAction("Banners");
    }

    [HttpPost, RequirePerm("banner.manage")]
    public IActionResult BannerDelete(int id)
    {
        var b = db.Banners.Find(id);
        if (b != null)
        {
            db.Banners.Remove(b);
            Audit("Xóa banner", b.Title);
            db.SaveChanges();
            TempData["Msg"] = "Đã xóa banner";
        }
        return RedirectToAction("Banners");
    }

    // ===== US18: nhật ký hệ thống (audit log) =====
    [RequirePerm("log.view")]
    public IActionResult SystemLog(string q)
    {
        var rows = db.AuditLogs.OrderByDescending(a => a.Id).Take(500).ToList();
        if (!string.IsNullOrWhiteSpace(q))
            rows = rows.Where(a => (a.UserEmail ?? "").Contains(q.Trim(), StringComparison.OrdinalIgnoreCase)
                || (a.Action ?? "").Contains(q.Trim(), StringComparison.OrdinalIgnoreCase)
                || (a.Detail ?? "").Contains(q.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        ViewBag.Q = q;
        return View(rows);
    }
}
