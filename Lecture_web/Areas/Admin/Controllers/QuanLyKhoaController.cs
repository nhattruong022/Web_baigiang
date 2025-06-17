using Microsoft.AspNetCore.Mvc;
using Lecture_web.Models;
using System.Linq;
using System;
using System.Collections.Generic;
using Lecture_web.Service;

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

        //Lấy danh sách khoa, tìm kiếm khoa
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

        //Phân trang
        public IActionResult PaginationPartial(int currentPage, int totalPages)
        {
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.ChangePageFunc = "changeKhoaPage";
            return PartialView("~/Views/Shared/_PaginationPartial.cshtml");
        }

        //return view
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        //Thêm Khoa
        [HttpPost]
        public IActionResult ThemKhoaAjax([FromBody] KhoaModels khoa)
        {
            khoa.TenKhoa = StringHelper.NormalizeString(khoa.TenKhoa);
            khoa.MoTa = StringHelper.NormalizeString(khoa.MoTa);

            // Dictionary chứa tất cả các lỗi
            var errors = new Dictionary<string, string>();

            // Kiểm tra trùng tên khoa
            if (_context.Khoa.Any(k => k.TenKhoa == khoa.TenKhoa))
            {
                errors.Add("tenKhoa", "Tên khoa đã tồn tại!");
                return Json(new { success = false, errors = errors });
            }

            if (ModelState.IsValid)
            {
                khoa.NgayTao = DateTime.Now;
                khoa.NgayCapNhat = DateTime.Now;
                _context.Khoa.Add(khoa);
                _context.SaveChanges();
                return Json(new { success = true, message = "Thêm khoa thành công" });
            }

            // Thêm các lỗi validation từ ModelState
            var modelErrors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key.Substring(0, 1).ToLower() + kvp.Key.Substring(1), // Chuyển key về camelCase
                    kvp => kvp.Value.Errors.First().ErrorMessage
                );

            return Json(new { success = false, errors = modelErrors });
        }

        //Sửa Khoa
        [HttpPost]
        public IActionResult SuaKhoaAjax([FromBody] KhoaModels khoa)
        {
            khoa.TenKhoa = StringHelper.NormalizeString(khoa.TenKhoa);
            khoa.MoTa = StringHelper.NormalizeString(khoa.MoTa);

            // Dictionary chứa tất cả các lỗi
            var errors = new Dictionary<string, string>();

            // Kiểm tra trùng tên khoa (trừ chính khoa đang sửa)
            if (_context.Khoa.Any(k => k.TenKhoa == khoa.TenKhoa && k.IdKhoa != khoa.IdKhoa))
            {
                errors.Add("tenKhoa", "Tên khoa đã tồn tại!");
                return Json(new { success = false, errors = errors });
            }

            if (ModelState.IsValid)
            {
                var existing = _context.Khoa.FirstOrDefault(x => x.IdKhoa == khoa.IdKhoa);
                if (existing == null)
                {
                    errors.Add("general", "Không tìm thấy khoa!");
                    return Json(new { success = false, errors = errors });
                }

                existing.TenKhoa = khoa.TenKhoa;
                existing.MoTa = khoa.MoTa;
                existing.NgayCapNhat = DateTime.Now;

                _context.Khoa.Update(existing);
                _context.SaveChanges();
                return Json(new { success = true, message = "Sửa khoa thành công" });
            }

            // Thêm các lỗi validation từ ModelState
            var modelErrors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key.Substring(0, 1).ToLower() + kvp.Key.Substring(1), // Chuyển key về camelCase
                    kvp => kvp.Value.Errors.First().ErrorMessage
                );

            return Json(new { success = false, errors = modelErrors });
        }

        //Xoa Khoa
        public IActionResult XoaKhoaAjax([FromBody] XoaKhoaModel model)
        {
            var khoa = _context.Khoa.FirstOrDefault(x => x.IdKhoa == model.IdKhoa);
            if (khoa == null)
                return Json(new { success = false, message = "Không tìm thấy khoa" });
            // Kiểm tra ràng buộc bộ môn
            var hasBoMon = _context.BoMon.Any(bm => bm.IdKhoa == model.IdKhoa);
            if (hasBoMon)
                return Json(new { success = false, message = "Không thể xóa khoa vì còn bộ môn liên kết!" });
            _context.Khoa.Remove(khoa);
            _context.SaveChanges();
            return Json(new { success = true, message = "Xóa khoa thành công" });
        }


        public class XoaKhoaModel
        {
            public int IdKhoa { get; set; }
        }
        
    }
} 