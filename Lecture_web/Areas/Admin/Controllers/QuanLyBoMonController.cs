using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Lecture_web.Models;
using Lecture_web.Service;
using System.Collections.Generic;

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
                .Join(_context.Khoa,
                    b => b.IdKhoa,
                    k => k.IdKhoa,
                    (b, k) => new {
                        b.IdBoMon,
                        b.TenBoMon,
                        b.MoTa,
                        b.IdKhoa,
                        TenKhoa = k.TenKhoa,
                        b.NgayTao,
                        b.NgayCapNhat
                    })
                .Where(b => string.IsNullOrEmpty(search) || b.TenBoMon.Contains(search));

            int total = boMons.Count();
            int totalPages = (int)Math.Ceiling((double)total / pageSize);
            if (totalPages < 1) totalPages = 1;

            var paged = boMons
                .OrderBy(b => b.IdBoMon)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

        [HttpPost]
        public IActionResult ThemBoMonAjax([FromBody] BoMonModels boMon)
        {
            boMon.TenBoMon = StringHelper.NormalizeString(boMon.TenBoMon);
            boMon.MoTa = StringHelper.NormalizeString(boMon.MoTa);

            // Dictionary chứa tất cả các lỗi
            var errors = new Dictionary<string, string>();

            // Kiểm tra trùng tên bộ môn
            if (_context.BoMon.Any(b => b.TenBoMon == boMon.TenBoMon))
            {
                errors.Add("tenBoMon", "Tên bộ môn đã tồn tại!");
                return Json(new { success = false, errors = errors });
            }

            if(ModelState.IsValid)
            {
                boMon.NgayTao = DateTime.Now;
                boMon.NgayCapNhat = DateTime.Now;
                _context.BoMon.Add(boMon);
                _context.SaveChanges();
                return Json(new { success = true, message = "Thêm bộ môn thành công!" });
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

        [HttpGet]
        public IActionResult GetBoMonById(int id)
        {
            var boMon = _context.BoMon.FirstOrDefault(b => b.IdBoMon == id);
            if (boMon == null)
                return NotFound();
            return Json(boMon);
        }

        [HttpPost]
        public IActionResult EditBoMonAjax([FromBody] BoMonModels boMon)
        {
            boMon.TenBoMon = StringHelper.NormalizeString(boMon.TenBoMon);
            boMon.MoTa = StringHelper.NormalizeString(boMon.MoTa);

            // Dictionary chứa tất cả các lỗi
            var errors = new Dictionary<string, string>();

            // Kiểm tra trùng tên bộ môn (trừ chính bộ môn đang sửa)
            if (_context.BoMon.Any(b => b.TenBoMon == boMon.TenBoMon && b.IdBoMon != boMon.IdBoMon))
            {
                errors.Add("tenBoMon", "Tên bộ môn đã tồn tại!");
                return Json(new { success = false, errors = errors });
            }

            if (ModelState.IsValid)
            {
                var existing = _context.BoMon.FirstOrDefault(b => b.IdBoMon == boMon.IdBoMon);
                if (existing == null)
                {
                    errors.Add("general", "Không tìm thấy bộ môn!");
                    return Json(new { success = false, errors = errors });
                }

                existing.TenBoMon = boMon.TenBoMon;
                existing.MoTa = boMon.MoTa;
                existing.IdKhoa = boMon.IdKhoa;
                existing.NgayCapNhat = DateTime.Now;

                _context.SaveChanges();
                return Json(new { success = true, message = "Cập nhật bộ môn thành công!" });
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

        [HttpGet]
        public IActionResult GetKhoaList()
        {
            var khoas = _context.Khoa
                .Select(k => new { k.IdKhoa, k.TenKhoa })
                .OrderBy(k => k.TenKhoa)
                .ToList();
            return Json(khoas);
        }

        [HttpPost]
        public IActionResult XoaBoMonAjax([FromBody] XoaBoMonModel model)
        {
            var boMon = _context.BoMon.FirstOrDefault(b => b.IdBoMon == model.IdBoMon);
            if (boMon == null)
                return Json(new { success = false, message = "Không tìm thấy bộ môn" });
            // Kiểm tra ràng buộc học phần
            var hasHocPhan = _context.HocPhan.Any(hp => hp.IdBoMon == model.IdBoMon);
            if (hasHocPhan)
                return Json(new { success = false, message = "Không thể xóa bộ môn vì còn học phần liên kết!" });
            _context.BoMon.Remove(boMon);
            _context.SaveChanges();
            return Json(new { success = true, message = "Xóa bộ môn thành công" });
        }

        public class XoaBoMonModel
        {
            public int IdBoMon { get; set; }
        }

        
    }
} 