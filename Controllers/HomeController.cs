using CuaHangGiaDung.Data;
using CuaHangGiaDung.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CuaHangGiaDung.Controllers;

public class HomeController(AppDb db) : AppController
{

    // Gắn điểm sao và số lượt đánh giá cho mọi sản phẩm trong danh sách.
    void AttachRatings(List<Product> products)
    {
        if (products == null || products.Count == 0) return;
        var ids = products.Select(p => p.Id).ToList();
        var stats = db.ProductReviews
            .Where(r => ids.Contains(r.ProductId))
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count(), Average = g.Average(r => (double)r.Rating) })
            .ToList()
            .ToDictionary(x => x.ProductId, x => x);
        foreach (var product in products)
        {
            if (stats.TryGetValue(product.Id, out var stat))
            {
                product.AverageRating = stat.Average;
                product.ReviewCount = stat.Count;
            }
            else
            {
                product.AverageRating = 0;
                product.ReviewCount = 0;
            }
        }
    }

    public IActionResult Index()
    {
        var all = db.Products.Where(p => !p.IsHidden).OrderBy(p => p.Id).ToList();
        ViewBag.Cats = all.Where(p => !string.IsNullOrEmpty(p.Category))
            .GroupBy(p => p.Category).Select(g => new CatInfo(g.Key, g.Count())).ToList();
        ViewBag.Banners = db.Banners.Where(b => b.IsActive).OrderBy(b => b.SortOrder).ThenBy(b => b.Id).ToList();
        var featured = all.Take(8).ToList();
        AttachRatings(featured);
        return View(featured);
    }

    // ----- Bài viết (chỉ bài đã đăng) -----
    public IActionResult Posts() => View(db.Posts.Where(p => p.IsPublished).OrderByDescending(p => p.Id).ToList());

    public IActionResult Post(int id)
    {
        var p = db.Posts.Find(id);
        return p == null || !p.IsPublished ? NotFound() : View(p);
    }

    // ----- Liên hệ / phản ánh gửi về cho Staff xử lý -----
    public IActionResult Contact() => View();

    [HttpPost]
    public IActionResult Contact(string type, string name, string email, string phone, string message)
    {
        name = (name ?? "").Trim(); email = (email ?? "").Trim(); phone = (phone ?? "").Trim(); message = (message ?? "").Trim();
        if (name == "" || message == "" || (email == "" && phone == ""))
        {
            TempData["Err"] = "Vui lòng nhập họ tên, nội dung và ít nhất email hoặc số điện thoại";
            return View();
        }
        if (message.Length > 2000) message = message.Substring(0, 2000);
        db.Contacts.Add(new Contact
        {
            Type = ContactSts.Types.Contains(type) ? type : "Liên hệ",
            Name = name.Length > 100 ? name.Substring(0, 100) : name,
            Email = email.Length > 150 ? email.Substring(0, 150) : email,
            Phone = phone.Length > 30 ? phone.Substring(0, 30) : phone,
            Message = message
        });
        db.SaveChanges();
        TempData["Msg"] = "Cảm ơn bạn! Chúng tôi đã nhận được và sẽ phản hồi sớm.";
        return RedirectToAction("Contact");
    }

    // Tìm kiếm + lọc theo danh mục và khoảng giá
    public IActionResult Products(string q, string[] cats, string[] prices)
    {
        cats ??= Array.Empty<string>();
        prices ??= Array.Empty<string>();
        var all = db.Products.Where(p => !p.IsHidden).OrderBy(p => p.Id).ToList();
        var items = all.Where(p =>
            (string.IsNullOrWhiteSpace(q) || p.Name.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase)) &&
            (cats.Length == 0 || cats.Contains(p.Category)) &&
            (prices.Length == 0 || prices.Any(k => InRange(k, p.Price)))).ToList();
        AttachRatings(items);
        return View(new ProductsVm
        {
            Items = items, Q = q, SelCats = cats, SelPrices = prices,
            Cats = all.Select(p => p.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList()
        });
    }

    static bool InRange(string k, decimal v) => k == "a" ? v < 500000 : k == "b" ? v >= 500000 && v <= 1000000 : k == "c" && v > 1000000;

    public IActionResult Detail(int id)
    {
        var p = db.Products.Find(id);
        if (p == null || p.IsHidden) return NotFound();
        var reviews = db.ProductReviews.Where(r => r.ProductId == id).OrderByDescending(r => r.Created).ToList();
        ViewBag.Reviews = reviews;
        ViewBag.ReviewCount = reviews.Count;
        ViewBag.AverageRating = reviews.Count == 0 ? 0.0 : reviews.Average(r => r.Rating);
        return View(p);
    }

    [HttpPost]
    public IActionResult AddReview(int productId, string customerName, int rating, string comment)
    {
        var product = db.Products.FirstOrDefault(p => p.Id == productId && !p.IsHidden);
        if (product == null) return NotFound();
        customerName = (customerName ?? "").Trim();
        comment = (comment ?? "").Trim();
        if (customerName.Length == 0 || comment.Length == 0 || rating < 1 || rating > 5)
        {
            TempData["Err"] = "Vui lòng nhập tên, bình luận và chọn số sao từ 1 đến 5.";
            return RedirectToAction(nameof(Detail), new { id = productId });
        }
        if (customerName.Length > 80) customerName = customerName[..80];
        if (comment.Length > 1200) comment = comment[..1200];
        db.ProductReviews.Add(new ProductReview
        {
            ProductId = productId,
            CustomerName = customerName,
            Rating = rating,
            Comment = comment,
            Created = DateTime.Now
        });
        db.SaveChanges();
        TempData["Msg"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
        return RedirectToAction(nameof(Detail), new { id = productId });
    }

    [Authorize]
    public IActionResult Favorites()
    {
        var fav = (HashSet<int>)ViewBag.Fav;
        var products = db.Products.Where(p => !p.IsHidden).OrderBy(p => p.Id).ToList().Where(p => fav.Contains(p.Id)).ToList();
        AttachRatings(products);
        return View(products);
    }

    [HttpPost, Authorize]
    public IActionResult ToggleFav(int id, string returnUrl)
    {
        var f = db.Favorites.FirstOrDefault(x => x.UserId == Uid && x.ProductId == id);
        if (f != null) db.Favorites.Remove(f);
        else if (db.Products.Any(p => p.Id == id && !p.IsHidden)) db.Favorites.Add(new Favorite { UserId = Uid, ProductId = id });
        db.SaveChanges();
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
