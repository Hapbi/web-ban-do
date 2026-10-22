using System.Security.Claims;
using CuaHangGiaDung.Data;
using CuaHangGiaDung.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace CuaHangGiaDung.Controllers;

public class AccountController(AppDb db) : AppController
{
    public IActionResult Login(string returnUrl) { ViewBag.Return = returnUrl; return View(); }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, string returnUrl)
    {
        var e = (email ?? "").Trim().ToLower();
        var u = db.Users.FirstOrDefault(x => x.Email.ToLower() == e);
        if (u == null || !Pw.Check(u, password ?? "")) { ViewBag.Err = "Sai email hoặc mật khẩu"; ViewBag.Return = returnUrl; return View(); }
        if (u.IsLocked) { ViewBag.Err = "Tài khoản đã bị khóa, vui lòng liên hệ quản trị viên"; ViewBag.Return = returnUrl; return View(); }
        await HttpContext.SignInAsync("Cookies", MakePrincipal(u));
        // Admin / Staff vào thẳng dashboard, khách hàng về trang chủ
        if (Perm.Backoffice.Contains(u.Role)) return RedirectToAction("Dashboard", "Manage");
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }

    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(string email, string fullName, string phone, string password)
    {
        var e = (email ?? "").Trim().ToLower();
        if (!e.Contains('@') || string.IsNullOrWhiteSpace(fullName)) { ViewBag.Err = "Vui lòng nhập họ tên và email hợp lệ"; return View(); }
        if ((password ?? "").Length < 6) { ViewBag.Err = "Mật khẩu tối thiểu 6 ký tự"; return View(); }
        if (db.Users.Any(x => x.Email.ToLower() == e)) { ViewBag.Err = "Email đã tồn tại"; return View(); }
        var u = new AppUser { Email = e, FullName = fullName.Trim(), Phone = (phone ?? "").Trim(), Role = "Customer" }; // luôn là khách hàng
        u.PasswordHash = Pw.Hash(u, password);
        db.Users.Add(u);
        db.SaveChanges();
        await HttpContext.SignInAsync("Cookies", MakePrincipal(u));
        return Redirect("/");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        return Redirect("/");
    }

    public IActionResult Denied() => View();

    public static ClaimsPrincipal MakePrincipal(AppUser u)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Name, u.FullName ?? u.Email),
            new(ClaimTypes.Email, u.Email ?? ""),
            new(ClaimTypes.Role, u.Role)
        };
        claims.AddRange(Perm.Effective(u.Role, u.Permissions).Select(k => new Claim(Helpers.Ext.PermClaim, k)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies"));
    }
}
