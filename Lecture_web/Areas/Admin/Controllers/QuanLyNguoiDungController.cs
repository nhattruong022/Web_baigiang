using Microsoft.AspNetCore.Mvc;
using Lecture_web.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using Newtonsoft.Json;


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


        // Hiển thị danh sách người dùng
        public IActionResult Index(int page = 1)
        {
            int pageSize = 5;
            var users = _context.TaiKhoan.ToList();
            int totalUsers = users.Count;
            int totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
            var pagedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View(pagedUsers);
        }




        // Lọc dữ liệu
        public IActionResult FilterAjax(string role, string status, string search, int page = 1)
        {
            int pageSize = 5;
            var users = _context.TaiKhoan.AsQueryable();

            if (!string.IsNullOrEmpty(role) && role != "all")
                users = users.Where(u => u.VaiTro == role);

            if (!string.IsNullOrEmpty(status) && status != "all")
                users = users.Where(u => u.TrangThai == status);

            if(!string.IsNullOrEmpty(search)){  
                users = users.Where(u => u.TenDangNhap.Contains(search));
            }

            int totalUsers = users.Count();
            int totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
            if (totalPages < 1) totalPages = 1;
            var pagedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return PartialView("_UserTablePartial", pagedUsers);
        }

        // Thêm dữ liệu
        [HttpPost]
        public IActionResult AddUserAjax(TaiKhoanModels model, IFormFile AnhDaiDien, int page = 1)
        {
            // Bỏ validate field file nếu không bắt buộc
            ModelState.Remove("AnhDaiDien");

            if (_context.TaiKhoan.Any(u => u.TenDangNhap == model.TenDangNhap))
            {
                // Trả về lỗi cho trường TenDangNhap
                return Json(new { success = false, errors = new { TenDangNhap = "Tên đăng nhập đã tồn tại!" } });
            }
            else if(_context.TaiKhoan.Any(u => u.Email == model.Email))
            {
                return Json(new { success = false, errors = new { Email = "Email đã tồn tại!" } });
            }
            else if(_context.TaiKhoan.Any(u => u.SoDienThoai == model.SoDienThoai))
            {
                return Json(new { success = false, errors = new { SoDienThoai = "Số điện thoại đã tồn tại!" } });
            }
            

            if (ModelState.IsValid)
            {
                model.TrangThai = "HoatDong";
                model.NgayTao = DateTime.Now;
                model.NgayCapNhat = DateTime.Now;
                
                // Xử lý lưu file ảnh nếu có
                if (AnhDaiDien != null && AnhDaiDien.Length > 0)
                {
                    var fileName = Path.GetFileName(AnhDaiDien.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        AnhDaiDien.CopyTo(stream);
                    }
                    model.AnhDaiDien = "/images/" + fileName;
                }

                _context.TaiKhoan.Add(model);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            else
            {
                // Trả về lỗi từng trường
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.First().ErrorMessage
                    );
                // Log lỗi ra console để debug
                System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(errors));
                return Json(new { success = false, errors });
            }
        }


        [HttpPost]
        public IActionResult EditUserAjax(TaiKhoanModels model, IFormFile AnhDaiDien, int page = 1)
        {
            // Bỏ validate field file nếu không bắt buộc
            ModelState.Remove("AnhDaiDien");

            // Kiểm tra trùng tên đăng nhập (trừ chính user đang sửa)
            if (_context.TaiKhoan.Any(u => u.TenDangNhap == model.TenDangNhap && u.IdTaiKhoan != model.IdTaiKhoan))
            {
                return Json(new { success = false, errors = new { TenDangNhap = "Tên đăng nhập đã tồn tại!" } });
            }
            // Kiểm tra trùng email
            else if (_context.TaiKhoan.Any(u => u.Email == model.Email && u.IdTaiKhoan != model.IdTaiKhoan))
            {
                return Json(new { success = false, errors = new { Email = "Email đã tồn tại!" } });
            }
            // Kiểm tra trùng số điện thoại
            else if (_context.TaiKhoan.Any(u => u.SoDienThoai == model.SoDienThoai && u.IdTaiKhoan != model.IdTaiKhoan))
            {
                return Json(new { success = false, errors = new { SoDienThoai = "Số điện thoại đã tồn tại!" } });
            }

            if (ModelState.IsValid)
            {
                var user = _context.TaiKhoan.FirstOrDefault(u => u.IdTaiKhoan == model.IdTaiKhoan);
                if (user == null)
                {
                    return Json(new { success = false, errors = new { General = "Người dùng không tồn tại!" } });
                }

                // Cập nhật thông tin
                user.TenDangNhap = model.TenDangNhap;
                user.MatKhau = model.MatKhau;
                user.HoTen = model.HoTen;
                user.VaiTro = model.VaiTro;
                user.Email = model.Email;
                user.SoDienThoai = model.SoDienThoai;
                user.TrangThai = model.TrangThai?.Trim();
                user.NgayCapNhat = DateTime.Now;

                // Xử lý lưu file ảnh nếu có
                if (AnhDaiDien != null && AnhDaiDien.Length > 0)
                {
                    var fileName = Path.GetFileName(AnhDaiDien.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        AnhDaiDien.CopyTo(stream);
                    }
                    user.AnhDaiDien = "/images/" + fileName;
                }

                _context.TaiKhoan.Update(user);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            else
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.First().ErrorMessage
                    );
                return Json(new { success = false, errors });
            }
        }
        

     [HttpGet]
        public IActionResult EditUserPartial(int id)
        {
            var user = _context.TaiKhoan.FirstOrDefault(u => u.IdTaiKhoan == id);
            if (user == null)
                return Content("Không tìm thấy người dùng");
            return PartialView("_EditUserPartial", user);
        }


        [HttpPost]
        public IActionResult UpdateStatusUser(int IdTaiKhoan, string TrangThai)
        {
            var user = _context.TaiKhoan.FirstOrDefault(u => u.IdTaiKhoan == IdTaiKhoan);
            if (user == null)
                return Json(new { success = false, errors = new { General = "Người dùng không tồn tại!" } });

            user.TrangThai = TrangThai?.Trim();
            _context.TaiKhoan.Update(user);
            _context.SaveChanges();

            return Json(new { success = true });
        }



        public IActionResult PaginationPartial(string role, string status, string search, int page = 1)
        {
            int pageSize = 5;
            var users = _context.TaiKhoan.AsQueryable();

            if (!string.IsNullOrEmpty(role) && role != "all")
                users = users.Where(u => u.VaiTro == role);
            if (!string.IsNullOrEmpty(status) && status != "all")
                users = users.Where(u => u.TrangThai == status);
            if (!string.IsNullOrEmpty(search))
                users = users.Where(u => u.TenDangNhap.Contains(search));

            // TÍNH SAU KHI ĐÃ LỌC
            int totalUsers = users.Count();
            int totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

            // Đảm bảo totalPages >= 1
            if (totalPages < 1) totalPages = 1;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return PartialView("_PaginationPartial");
        }



   



        
    }
} 