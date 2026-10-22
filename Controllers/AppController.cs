using System.Security.Claims;
using CuaHangGiaDung.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CuaHangGiaDung.Controllers;

// Lớp cha của mọi controller: tiện ích dùng chung
public abstract class AppController : Controller
{
    protected int Uid => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    // Danh sách sản phẩm yêu thích của người đang đăng nhập (để tô đỏ trái tim ở các thẻ sản phẩm)
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var fav = new HashSet<int>();
        if (User.Identity?.IsAuthenticated == true)
        {
            var db = HttpContext.RequestServices.GetRequiredService<AppDb>();
            var uid = Uid;
            fav = db.Favorites.Where(f => f.UserId == uid).Select(f => f.ProductId).ToHashSet();
        }
        ViewBag.Fav = fav;
        base.OnActionExecuting(context);
    }
}
