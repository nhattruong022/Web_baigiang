using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Lecture_web.Models;

namespace Lecture_web.Areas.Admin.Controllers
{
    
    [Area("Admin")]
    public class QuanLyBoMonController : Controller
    {
        private readonly ApplicationDbContext _context;
        public QuanLyBoMonController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetBoMonList(string search = "", int page = 1)
        {
            int pageSize = 5;
            var boMons = _context.BoMon
                .Where(b => string.IsNullOrEmpty(search) || b.TenBoMon.Contains(search));
            int total = boMons.Count();
            int totalPages = (int)Math.Ceiling((double)total / pageSize);
            if (totalPages < 1) totalPages = 1;
            var paged = boMons
                .OrderBy(b => b.IdBoMon)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new {
                    b.IdBoMon,
                    b.TenBoMon,
                    b.MoTa,
                    b.IdKhoa,
                    b.NgayTao,
                    b.NgayCapNhat
                })
                .ToList();

            return Json(new {
                data = paged,
                currentPage = page,
                totalPages = totalPages
            });
        }

        public IActionResult PaginationPartial(int currentPage, int totalPages)
        {
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.ChangePageFunc = "changeBoMonPage";
            return PartialView("~/Views/Shared/_PaginationPartial.cshtml");
        }
    }
} 