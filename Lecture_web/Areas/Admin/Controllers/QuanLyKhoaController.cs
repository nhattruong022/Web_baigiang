using Microsoft.AspNetCore.Mvc;
using Lecture_web.Models;
using System.Linq;
using System;

namespace Lecture_web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class QuanLyKhoaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuanLyKhoaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // AJAX: Lấy danh sách khoa, có thể truyền từ khóa tìm kiếm
        [HttpGet]
        public IActionResult GetKhoaList(string search = "", int page = 1)
        {
            int pageSize = 5;
            var khoas = _context.Khoa
                .Where(k => string.IsNullOrEmpty(search) || k.TenKhoa.Contains(search));
            int totalKhoa = khoas.Count();
            int totalPages = (int)Math.Ceiling((double)totalKhoa / pageSize);
            if (totalPages < 1) totalPages = 1;
            var pagedKhoa = khoas
                .OrderBy(k => k.IdKhoa)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Json(new {
                data = pagedKhoa,
                currentPage = page,
                totalPages = totalPages
            });
        }

        public IActionResult PaginationPartial(int currentPage, int totalPages)
        {
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.ChangePageFunc = "changeKhoaPage";
            return PartialView("~/Views/Shared/_PaginationPartial.cshtml");
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public IActionResult ThemKhoaAjax([FromBody] KhoaModels khoa)
        {
            if (ModelState.IsValid)
            {
                khoa.NgayTao = DateTime.Now;
                khoa.NgayCapNhat = DateTime.Now;
                _context.Khoa.Add(khoa);
                _context.SaveChanges();
                return Json(new { success = true, message = "Thêm khoa thành công" });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
        }

        [HttpPost]
        public IActionResult SuaKhoaAjax([FromBody] KhoaModels khoa)
        {
            if (ModelState.IsValid)
            {
                // Lấy bản ghi cũ từ DB
                var existing = _context.Khoa.FirstOrDefault(x => x.IdKhoa == khoa.IdKhoa);
                if (existing == null)
                    return Json(new { success = false, message = "Không tìm thấy khoa" });

                // Cập nhật các trường cho phép sửa
                existing.TenKhoa = khoa.TenKhoa;
                existing.MoTa = khoa.MoTa;
                existing.NgayCapNhat = DateTime.Now; // cập nhật ngày cập nhật

                _context.Khoa.Update(existing);
                _context.SaveChanges();
                return Json(new { success = true, message = "Sửa khoa thành công" });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
        }

        
    }
} 