using Microsoft.AspNetCore.Mvc;
using Lecture_web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Lecture_web.Controllers
{
    public class profileController : Controller
    {
        private readonly ApplicationDbContext _context;
        public profileController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult profile()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");
            var user = _context.TaiKhoan.FirstOrDefault(u => u.TenDangNhap == username);
            if (user == null)
                return RedirectToAction("Login", "Account");
            ViewBag.Avatar = !string.IsNullOrEmpty(user.AnhDaiDien) ? user.AnhDaiDien : "/images/default-avatar.png";
            ViewBag.UserName = user.HoTen ?? user.TenDangNhap;
            return View(user);
        }

        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }


            [HttpPost]
        public IActionResult EditProfilAjax(string HoTen, string Email, string SDT)
            {
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return RedirectToAction("Login", "Account");
                var user = _context.TaiKhoan.FirstOrDefault(u => u.TenDangNhap == username);
                if (user == null)
                    return RedirectToAction("Login", "Account");
                user.HoTen = HoTen;
                user.Email = Email;
                user.SoDienThoai = SDT;
                _context.TaiKhoan.Update(user);
                _context.SaveChanges();
                return RedirectToAction("profile");
            }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username) || avatar == null || avatar.Length == 0)
                return Json(new { success = false, message = "Không có file ảnh." });
            var user = _context.TaiKhoan.FirstOrDefault(u => u.TenDangNhap == username);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy user." });
            var fileName = $"avatar_{user.IdTaiKhoan}_{DateTime.Now.Ticks}{System.IO.Path.GetExtension(avatar.FileName)}";
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/avatars", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }
            user.AnhDaiDien = $"/images/avatars/{fileName}";
            _context.TaiKhoan.Update(user);
            _context.SaveChanges();
            return Json(new { success = true, url = user.AnhDaiDien });
        }

        [HttpPost]
        public IActionResult ChangePassword(string oldPassword, string newPassword)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Tài khoản không tồn tại" });
            var user = _context.TaiKhoan.FirstOrDefault(u => u.TenDangNhap == username);
            if (user == null)
                return Json(new { success = false, message = "Tài khoản không tồn tại" });
            if (user.MatKhau != oldPassword)
                return Json(new { success = false, message = "Mật khẩu cũ không đúng" });
            user.MatKhau = newPassword;
            _context.TaiKhoan.Update(user);
            _context.SaveChanges();
            return Json(new { success = true, message = "Đổi mật khẩu thành công" });
        }
        
       
    }
} 