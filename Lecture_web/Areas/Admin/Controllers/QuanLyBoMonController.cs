using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Lecture_web.Models;
using Lecture_web.Service;

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

        [HttpPost]
        public IActionResult ThemBoMonAjax([FromBody] BoMonModels boMon)
        {
            boMon.TenBoMon = StringHelper.NormalizeString(boMon.TenBoMon);
            boMon.MoTa = StringHelper.NormalizeString(boMon.MoTa);
            if(ModelState.IsValid)
            {
                boMon.NgayTao = DateTime.Now;
                boMon.NgayCapNhat = DateTime.Now;
                _context.BoMon.Add(boMon);
                _context.SaveChanges();
                return Json(new { success = true, message = "Thêm bộ môn thành công!" });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
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
            if (ModelState.IsValid)
            {
                var existing = _context.BoMon.FirstOrDefault(b => b.IdBoMon == boMon.IdBoMon);
                if (existing == null)
                    return Json(new { success = false, message = "Không tìm thấy bộ môn!" });

                existing.TenBoMon = boMon.TenBoMon;
                existing.MoTa = boMon.MoTa;
                existing.IdKhoa = boMon.IdKhoa;
                existing.NgayCapNhat = DateTime.Now;

                _context.SaveChanges();
                return Json(new { success = true, message = "Cập nhật bộ môn thành công!" });
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
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