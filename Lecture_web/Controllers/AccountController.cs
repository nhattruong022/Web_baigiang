using Microsoft.AspNetCore.Mvc;
using Lecture_web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Net.Mail;
using System.Net;
using Lecture_web.Service;

namespace Lecture_web.Controllers
{
    public class AccountController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        public AccountController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            var user = _context.TaiKhoan.FirstOrDefault(u => u.TenDangNhap == username);
            if (user == null || user.MatKhau != password)
            {
                ModelState.AddModelError("username", "Tên đăng nhập hoặc mật khẩu không đúng.");
                ModelState.AddModelError("password", "Tên đăng nhập hoặc mật khẩu không đúng.");
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }
            if (user.TrangThai != null && user.TrangThai.Trim() == "KhongHoatDong")
            {
                ModelState.AddModelError("username", "Tài khoản của bạn đã bị khóa hoặc không hoạt động.");
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.IdTaiKhoan.ToString()),
                new Claim(ClaimTypes.Name, user.TenDangNhap ?? ""),
                new Claim(ClaimTypes.Role, user.VaiTro ?? "")
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            // Nếu có returnUrl, redirect về đó
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Redirect theo role (mặc định)
            if (user.VaiTro == "Admin")
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            if (user.VaiTro == "Giangvien")
                return RedirectToAction("Index", "LopHoc", new { area = "User" });
            if (user.VaiTro == "Sinhvien")
                return RedirectToAction("Index", "LopHoc", new { area = "User" });

            return RedirectToAction("Login");
        }

        // Thêm hàm làm sạch email
        private string CleanEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return "";
            // Loại bỏ các ký tự invisible Unicode và khoảng trắng
            return new string(email.Where(c => c != '\u200B' && c != '\u200C' && c != '\u200D' && c != '\uFEFF').ToArray()).Trim().ToLower();
        }

        [HttpPost]
        public async Task<JsonResult> SendOTP(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return Json(new { success = false, message = "Vui lòng nhập email!" });

                var emailCheck = CleanEmail(email);
                var user = _context.TaiKhoan.AsEnumerable().FirstOrDefault(u => CleanEmail(u.Email) == emailCheck);
                if (user == null)
                    return Json(new { success = false, message = "Email không tồn tại!" });

                var otp = new Random().Next(100000, 999999).ToString();
                var otpModel = new OtpModels
                {
                    IdTaiKhoan = user.IdTaiKhoan,
                    OtpCode = otp,
                    NgayTao = DateTime.Now,
                    NgayHetHan = DateTime.Now.AddMinutes(5)
                };
                _context.Add(otpModel);
                _context.SaveChanges();

                // Gửi email thực tế qua service (bây giờ là async)
                await _emailService.SendEmailAsync(email, "Mã xác thực OTP", $"Mã OTP của bạn là: {otp}");

                return Json(new { success = true, message = "Đã gửi mã xác thực về email!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi gửi email: " + ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine("Chi tiết: " + ex.InnerException.Message);
                throw; // hoặc return false, hoặc trả về lỗi chi tiết cho client
            }
        }

        [HttpPost]
        public IActionResult VerifyOTP(string email, string otp)
        {
            var emailCheck = CleanEmail(email);
            var user = _context.TaiKhoan.AsEnumerable().FirstOrDefault(u => CleanEmail(u.Email) == emailCheck);
            if (user == null)
                return Json(new { success = false, message = "Email không tồn tại!" });
            var otpModel = _context.Set<OtpModels>()
                .Where(o => o.IdTaiKhoan == user.IdTaiKhoan && o.OtpCode == otp && o.NgayHetHan > DateTime.Now)
                .OrderByDescending(o => o.NgayTao)
                .FirstOrDefault();
            if (otpModel == null)
                return Json(new { success = false, message = "Mã OTP không hợp lệ hoặc đã hết hạn!" });
            // Xóa OTP sau khi xác thực thành công
            _context.Remove(otpModel);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ResetPassword(string email, string newPassword)
        {
            var emailCheck = CleanEmail(email);
            var user = _context.TaiKhoan.AsEnumerable().FirstOrDefault(u => CleanEmail(u.Email) == emailCheck);
            if (user == null)
                return Json(new { success = false, message = "Email không tồn tại!" });
            user.MatKhau = newPassword;
            _context.TaiKhoan.Update(user);
            _context.SaveChanges();
            return Json(new { success = true, message = "Đặt lại mật khẩu thành công!" });
        }

            [HttpGet]
            public IActionResult ForgotPassword()
            {
                return View();
            }
        
        
    }
} 