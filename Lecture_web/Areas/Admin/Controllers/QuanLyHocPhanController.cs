using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System;
using Lecture_web.Models;

namespace Lecture_web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class QuanLyHocPhanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuanLyHocPhanController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetHocPhanList(string search = "", int page = 1)
        {
            int pageSize = 2;
            var hocPhans = _context.HocPhan
                .Where(h => string.IsNullOrEmpty(search) || h.TenHocPhan.Contains(search));
            int total = hocPhans.Count();
            int totalPages = (int)Math.Ceiling((double)total / pageSize);
            if (totalPages < 1) totalPages = 1;
            var pagedHocPhan = hocPhans
                .OrderBy(h => h.IdHocPhan)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new {
                    h.IdHocPhan,
                    h.TenHocPhan,
                    h.MoTa,
                    h.TrangThai,
                    h.NgayTao,
                    h.NgayCapNhat,
                    BoMon = h.BoMon != null ? h.BoMon.TenBoMon : "",
                })
                .ToList();

         return Json(new {
                data = pagedHocPhan,
                currentPage = page,
                totalPages = totalPages
            });
        }

        public IActionResult PaginationPartial(int currentPage, int totalPages)
        {
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.ChangePageFunc = "changeHocPhanPage";
            return PartialView("~/Views/Shared/_PaginationPartial.cshtml");
        }
    }
} 