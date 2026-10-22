using CuaHangGiaDung.Data;
using CuaHangGiaDung.Helpers;
using CuaHangGiaDung.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CuaHangGiaDung.Controllers;

[Authorize]
public class OrderController(AppDb db) : AppController
{
    public IActionResult Checkout()
    {
        var lines = CartHelper.Lines(HttpContext.Session, db);
        if (lines.Count == 0) return RedirectToAction("Index", "Cart");
        ViewBag.User = db.Users.Find(Uid);
        return View(lines);
    }

    [HttpPost]
    public IActionResult Checkout(string receiver, string phone, string address, string method)
    {
        var lines = CartHelper.Lines(HttpContext.Session, db);
        if (lines.Count == 0) return RedirectToAction("Index", "Cart");
        if (string.IsNullOrWhiteSpace(receiver) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(address))
        {
            TempData["Err"] = "Vui lòng nhập đủ thông tin nhận hàng";
            return RedirectToAction("Checkout");
        }
        var cod = method == PayM.COD;
        var o = new Order
        {
            UserId = Uid, Receiver = receiver.Trim(), Phone = phone.Trim(), Address = address.Trim(),
            PaymentMethod = cod ? PayM.COD : PayM.QR,
            Status = cod ? Sts.All[1] : Sts.All[0] // COD: vào thẳng "Chờ xác nhận" để shop gọi xác nhận
        };
        foreach (var l in lines)
        {
            if (l.Qty > l.P.Stock)
            {
                TempData["Err"] = $"'{l.P.Name}' chỉ còn {l.P.Stock} sản phẩm";
                return RedirectToAction("Index", "Cart");
            }
            l.P.Stock -= l.Qty;
            o.Total += l.P.Price * l.Qty; // giá luôn lấy từ database
            o.Items.Add(new OrderItem { ProductId = l.P.Id, ProductName = l.P.Name, Price = l.P.Price, Qty = l.Qty });
        }
        db.Orders.Add(o);
        db.SaveChanges();
        CartHelper.Save(HttpContext.Session, new());
        return cod ? RedirectToAction("Done", new { id = o.Id }) : RedirectToAction("Pay", new { id = o.Id });
    }

    Order Find(int id)
    {
        var o = db.Orders.Include(x => x.Items).FirstOrDefault(x => x.Id == id);
        return o != null && (o.UserId == Uid || User.Can("order.view")) ? o : null;
    }

    public IActionResult Pay(int id)
    {
        var o = Find(id);
        if (o == null) return NotFound();
        if (o.PaymentMethod == PayM.COD) return RedirectToAction("Done", new { id });
        if (o.Status != Sts.All[0]) return RedirectToAction("Detail", new { id }); // đã thanh toán / đã xử lý -> xem hóa đơn
        ViewBag.Shop = db.Settings.First();
        return View(o);
    }

    // Chi tiết đơn hàng = hóa đơn
    public IActionResult Detail(int id)
    {
        var o = Find(id);
        if (o == null) return NotFound();
        ViewBag.Shop = db.Settings.First();
        return View(o);
    }

    [HttpPost]
    public IActionResult Paid(int id)
    {
        var o = db.Orders.FirstOrDefault(x => x.Id == id && x.UserId == Uid);
        if (o == null) return NotFound();
        if (o.PaymentMethod != PayM.COD && o.Status == Sts.All[0]) { o.Status = Sts.All[1]; db.SaveChanges(); }
        return RedirectToAction("Done", new { id });
    }

    public IActionResult Done(int id)
    {
        var o = Find(id);
        return o == null ? NotFound() : View(o);
    }

    public IActionResult My(string tab)
    {
        ViewBag.Tab = string.IsNullOrEmpty(tab) ? "Tất cả" : tab;
        return View(db.Orders.Include(o => o.Items).Where(o => o.UserId == Uid).OrderByDescending(o => o.Id).ToList());
    }
}
