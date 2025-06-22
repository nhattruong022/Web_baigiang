using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Lecture_web.Models;
using System.Linq;

namespace Lecture_web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var username = User.Identity?.Name;
            var user = _context.TaiKhoan.FirstOrDefault(u => u.TenDangNhap == username);
            ViewBag.Avatar = user != null && !string.IsNullOrEmpty(user.AnhDaiDien) ? user.AnhDaiDien : "/images/avatar.jpg";
            ViewBag.UserName = user?.HoTen ?? user?.TenDangNhap ?? "User";
            ViewBag.TotalKhoa = _context.Khoa.Count();
            ViewBag.TotalTaiKhoan = _context.TaiKhoan.Count();
            ViewBag.TotalHocPhan = _context.HocPhan.Count();
            ViewBag.TotalBoMon = _context.BoMon.Count();

            return View();
        }


        
    }



} 