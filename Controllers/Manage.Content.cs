using CuaHangGiaDung.Helpers;
using CuaHangGiaDung.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CuaHangGiaDung.Controllers;

// US14 (Staff): bài viết, liên hệ & phản ánh  +  báo cáo vận hành
public partial class ManageController
{
    // ----- Bài viết -----
    public IActionResult Posts()
    {
        if (!User.Can("post.manage")) return Forbid();
        return View(db.Posts.OrderByDescending(p => p.Id).ToList());
    }

    public IActionResult PostForm(int? id)
    {
        if (!User.Can("post.manage")) return Forbid();
        var p = id == null ? new Post { IsPublished = true } : db.Posts.Find(id);
        return p == null ? NotFound() : View(p);
    }

    [HttpPost]
    public IActionResult PostForm(Post p)
    {
        if (!User.Can("post.manage")) return Forbid();
        var img = (p.ImageUrl ?? "").Trim();
        if (img != "" && !img.StartsWith("http://") && !img.StartsWith("https://") && !img.StartsWith("/"))
            ModelState.AddModelError("", "Link ảnh phải bắt đầu bằng http:// hoặc https://");
        if (string.IsNullOrWhiteSpace(p.Title)) ModelState.AddModelError("", "Vui lòng nhập tiêu đề bài viết");
        if (!ModelState.IsValid) return View(p);

        Post t;
        if (p.Id == 0) { t = new Post { Author = User.Identity?.Name }; db.Posts.Add(t); }
        else { t = db.Posts.Find(p.Id); if (t == null) return NotFound(); }
        t.Title = p.Title.Trim();
        t.Summary = (p.Summary ?? "").Trim();
        t.Content = (p.Content ?? "").Trim();
        t.ImageUrl = img;
        t.IsPublished = p.IsPublished;
        Audit(p.Id == 0 ? "Thêm bài viết" : "Sửa bài viết", t.Title);
        db.SaveChanges();
        TempData["Msg"] = "Đã lưu bài viết";
        return RedirectToAction("Posts");
    }

    [HttpPost]
    public IActionResult TogglePost(int id)
    {
        if (!User.Can("post.manage")) return Forbid();
        var p = db.Posts.Find(id);
        if (p == null) return NotFound();
        p.IsPublished = !p.IsPublished;
        Audit(p.IsPublished ? "Đăng bài viết" : "Ẩn bài viết", p.Title);
        db.SaveChanges();
        TempData["Msg"] = p.IsPublished ? "Đã đăng bài viết" : "Đã ẩn bài viết";
        return RedirectToAction("Posts");
    }

    [HttpPost]
    public IActionResult DeletePost(int id)
    {
        if (!User.Can("post.manage")) return Forbid();
        var p = db.Posts.Find(id);
        if (p != null)
        {
            db.Posts.Remove(p);
            Audit("Xóa bài viết", p.Title);
            db.SaveChanges();
            TempData["Msg"] = "Đã xóa bài viết";
        }
        return RedirectToAction("Posts");
    }

    // ----- Liên hệ & phản ánh -----
    public IActionResult Contacts(string type, string status)
    {
        if (!User.Can("contact.manage")) return Forbid();
        var q = db.Contacts.AsQueryable();
        if (!string.IsNullOrEmpty(type)) q = q.Where(c => c.Type == type);
        if (!string.IsNullOrEmpty(status)) q = q.Where(c => c.Status == status);
        ViewBag.Type = type;
        ViewBag.Status = status;
        ViewBag.NewCount = db.Contacts.Count(c => c.Status == "Mới");
        return View(q.OrderByDescending(c => c.Id).ToList());
    }

    [HttpPost]
    public IActionResult ContactUpdate(int id, string status, string note)
    {
        if (!User.Can("contact.manage")) return Forbid();
        var c = db.Contacts.Find(id);
        if (c == null) return NotFound();
        if (!ContactSts.All.Contains(status)) return BadRequest();
        c.Status = status;
        c.StaffNote = (note ?? "").Trim();
        c.HandledBy = User.Identity?.Name;
        Audit("Xử lý " + c.Type.ToLower(), $"#{c.Id} {c.Name}: {status}");
        db.SaveChanges();
        TempData["Msg"] = "Đã cập nhật";
        return RedirectToAction("Contacts");
    }

    // ----- Báo cáo vận hành (Staff + Admin) -----
    public IActionResult Report()
    {
        if (!User.Can("report.view")) return Forbid();
        var today = DateTime.Today;
        var orders = db.Orders.Include(o => o.Items).ToList();
        ViewBag.Today = today;
        ViewBag.OrdersToday = orders.Count(o => o.Created.Date == today);
        ViewBag.ByStatus = Sts.All.ToDictionary(s => s, s => orders.Count(o => o.Status == s));
        ViewBag.NeedConfirm = orders.Where(o => o.Status == Sts.All[1]).OrderBy(o => o.Created).Take(10).ToList();
        ViewBag.OldPending = orders.Count(o => o.Status == Sts.All[0] && o.Created < DateTime.Now.AddDays(-1));
        ViewBag.Shipping = orders.Count(o => o.Status == "Đang giao");
        ViewBag.NewContacts = db.Contacts.Count(c => c.Status == "Mới" && c.Type == "Liên hệ");
        ViewBag.NewFeedback = db.Contacts.Count(c => c.Status == "Mới" && c.Type == "Phản ánh");
        ViewBag.Processing = db.Contacts.Count(c => c.Status == "Đang xử lý");
        ViewBag.DraftPosts = db.Posts.Count(p => !p.IsPublished);
        var prods = db.Products.ToList();
        ViewBag.OutOfStock = prods.Where(p => p.Stock <= 0 && !p.IsHidden).OrderBy(p => p.Name).ToList();
        ViewBag.LowStock = prods.Where(p => p.Stock > 0 && p.Stock <= 5 && !p.IsHidden).OrderBy(p => p.Stock).ToList();
        return View();
    }
}
