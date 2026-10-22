using CuaHangGiaDung.Data;
using CuaHangGiaDung.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace CuaHangGiaDung.Controllers;

public class CartController(AppDb db) : AppController
{
    public IActionResult Index() => View(CartHelper.Lines(HttpContext.Session, db));

    [HttpPost]
    public IActionResult Add(int id, bool buy, string returnUrl)
    {
        var p = db.Products.Find(id);
        if (p == null || p.IsHidden) return NotFound();
        var c = CartHelper.Get(HttpContext.Session);
        var q = c.GetValueOrDefault(id) + 1;
        if (q > p.Stock)
        {
            TempData["Err"] = "Đã hết số lượng trong kho";
            if (!buy) return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/Cart");
        }
        else
        {
            c[id] = q;
            CartHelper.Save(HttpContext.Session, c);
            TempData["Msg"] = "Đã thêm vào giỏ hàng";
        }
        if (buy) return RedirectToAction("Checkout", "Order");
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/Cart");
    }

    [HttpPost]
    public IActionResult Change(int id, int delta)
    {
        var p = db.Products.Find(id);
        var c = CartHelper.Get(HttpContext.Session);
        var q = c.GetValueOrDefault(id) + delta;
        if (p == null || p.IsHidden || q <= 0) c.Remove(id); else c[id] = Math.Min(q, p.Stock);
        CartHelper.Save(HttpContext.Session, c);
        return RedirectToAction("Index");
    }
}
