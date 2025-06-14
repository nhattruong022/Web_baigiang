using Microsoft.AspNetCore.Mvc;
using Lecture_web.Models;
using System.Linq;

namespace Lecture_web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            ViewBag.TotalKhoa = _context.Khoa.Count();
            ViewBag.TotalTaiKhoan = _context.TaiKhoan.Count();
            ViewBag.TotalHocPhan = _context.HocPhan.Count();
            ViewBag.TotalBoMon = _context.BoMon.Count();
            return View();
        }


        
    }



} 