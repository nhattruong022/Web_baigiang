using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System;
using Lecture_web.Models;
using Lecture_web.Service;
using System.Collections.Generic;

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
            ViewBag.BoMons = _context.BoMon.ToList();
            return View();
        }

        [HttpGet]
        public IActionResult GetHocPhanList(string search = "", int page = 1, string status = "all")
        {
            int pageSize = 5;
            var hocPhans = _context.HocPhan
                .Where(h => (string.IsNullOrEmpty(search) || h.TenHocPhan.Contains(search))
                    && (status == "all" || h.TrangThai == status));
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

        [HttpGet]
        public IActionResult GetHocPhanById(int id)
        {
            var hp = _context.HocPhan.FirstOrDefault(h => h.IdHocPhan == id);
            if (hp == null) return NotFound();

            // Kiểm tra nếu trạng thái là Inactive
            if (hp.TrangThai?.Trim().ToLower() == "inactive")
            {
                return Json(new { error = true, message = "Không thể chỉnh sửa học phần đang ở trạng thái Inactive!" });
            }

            return Json(hp);
        }

        [HttpPost]
        public IActionResult ThemHocPhanAjax([FromBody] HocPhanModels hocPhan)
        {
            hocPhan.TenHocPhan = StringHelper.NormalizeString(hocPhan.TenHocPhan);
            hocPhan.MoTa = StringHelper.NormalizeString(hocPhan.MoTa);

            // Dictionary chứa tất cả các lỗi
            var errors = new Dictionary<string, string>();

            // Kiểm tra trùng tên học phần
            if (_context.HocPhan.Any(h => h.TenHocPhan == hocPhan.TenHocPhan))
            {
                errors.Add("tenHocPhan", "Tên học phần đã tồn tại!");
                return Json(new { success = false, errors = errors });
            }

            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(hocPhan.TrangThai))
                {
                    hocPhan.TrangThai = "Active";
                }
                
                hocPhan.NgayTao = DateTime.Now;
                hocPhan.NgayCapNhat = DateTime.Now;
                _context.HocPhan.Add(hocPhan);
                _context.SaveChanges();
                return Json(new { success = true, message = "Thêm học phần thành công!" });
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

        [HttpPost]
        public IActionResult EditHocPhanAjax([FromBody] HocPhanModels hocPhan)
        {
            hocPhan.TenHocPhan = StringHelper.NormalizeString(hocPhan.TenHocPhan);
            hocPhan.MoTa = StringHelper.NormalizeString(hocPhan.MoTa);

            // Dictionary chứa tất cả các lỗi
            var errors = new Dictionary<string, string>();

            // Kiểm tra trùng tên học phần (trừ chính học phần đang sửa)
            if (_context.HocPhan.Any(h => h.TenHocPhan == hocPhan.TenHocPhan && h.IdHocPhan != hocPhan.IdHocPhan))
            {
                errors.Add("tenHocPhan", "Tên học phần đã tồn tại!");
                return Json(new { success = false, errors = errors });
            }

            if (ModelState.IsValid)
            {
                var existing = _context.HocPhan.FirstOrDefault(h => h.IdHocPhan == hocPhan.IdHocPhan);
                if (existing == null)
                {
                    errors.Add("general", "Không tìm thấy học phần!");
                    return Json(new { success = false, errors = errors });
                }

                existing.TenHocPhan = hocPhan.TenHocPhan;
                existing.MoTa = hocPhan.MoTa;
                existing.IdBoMon = hocPhan.IdBoMon;
                existing.NgayCapNhat = DateTime.Now;

                _context.SaveChanges();
                return Json(new { success = true, message = "Cập nhật học phần thành công!" });
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

        [HttpPost]
        public IActionResult UpdateTrangThai([FromBody] UpdateTrangThaiHocPhanModel model)
        {
            var hocPhan = _context.HocPhan.FirstOrDefault(h => h.IdHocPhan == model.Id);
            if (hocPhan == null)
                return Json(new { success = false, message = "Không tìm thấy học phần!" });
         

            hocPhan.TrangThai = model.TrangThai;
            hocPhan.NgayCapNhat = DateTime.Now;
            _context.SaveChanges();
            return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
        }
    }

    public class UpdateTrangThaiHocPhanModel
    {
        public int Id { get; set; }
        public string TrangThai { get; set; }
    }
} 