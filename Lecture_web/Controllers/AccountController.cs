using Microsoft.AspNetCore.Mvc;
using Lecture_web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Lecture_web.Controllers
{
    public class AccountController : Controller
    {

        private readonly ApplicationDbContext _context;
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = _context.TaiKhoan.FirstOrDefault(u => u.TenDangNhap == username);
            if (user == null || user.MatKhau != password)
            {
                ModelState.AddModelError("username", "Tên đăng nhập hoặc mật khẩu không đúng.");
                ModelState.AddModelError("password", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View();
            }
            if (user.TrangThai != null && user.TrangThai.Trim() == "KhongHoatDong")
            {
                ModelState.AddModelError("username", "Tài khoản của bạn đã bị khóa hoặc không hoạt động.");
                return View();
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.TenDangNhap ?? ""),
                new Claim(ClaimTypes.Role, user.VaiTro ?? "")
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            // Redirect theo role
            if (user.VaiTro == "Admin")
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            if (user.VaiTro == "Giangvien")
                return RedirectToAction("Index", "LopHoc", new { area = "User" });
            if (user.VaiTro == "Sinhvien")
                return RedirectToAction("Index", "LopHoc", new { area = "User" });

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

      
    }
} 