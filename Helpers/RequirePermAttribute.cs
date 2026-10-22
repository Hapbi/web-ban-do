using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CuaHangGiaDung.Helpers;

// Chặn truy cập action nếu tài khoản không có quyền chức năng tương ứng (Admin luôn qua).
// Chưa đăng nhập -> chuyển tới trang đăng nhập; đã đăng nhập nhưng thiếu quyền -> trang "Không có quyền".
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermAttribute(string perm) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext ctx)
    {
        var u = ctx.HttpContext.User;
        if (u.Identity?.IsAuthenticated != true) { ctx.Result = new ChallengeResult(); return; }
        if (!u.Can(perm)) ctx.Result = new ForbidResult();
    }
}
