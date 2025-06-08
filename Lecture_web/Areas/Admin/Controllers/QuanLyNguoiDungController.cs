using Microsoft.AspNetCore.Mvc;
using Lecture_web.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Lecture_web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class QuanLyNguoiDungController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuanLyNguoiDungController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Lấy dữ liệu từ bảng TaiKhoan
            var users = _context.TaiKhoan.ToList();
            return View(users);
        }
    }
} 