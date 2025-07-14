using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Lecture_web.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Lecture_web.Service;

namespace Lecture_web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class QuanLyNguoiDungController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ExcelService _excelService;
        private readonly IEmailService _emailService;

        public QuanLyNguoiDungController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _excelService = new ExcelService();
            _emailService = emailService;
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
        public async Task<IActionResult> AddUserAjax(TaiKhoanModels model, IFormFile AnhDaiDien)
        {
            // Bỏ validate field file nếu không bắt buộc
            ModelState.Remove("AnhDaiDien");

            // Dictionary chứa tất cả các lỗi
            var errors = new Dictionary<string, string>();

            // Kiểm tra trùng tên đăng nhập
            if (_context.TaiKhoan.Any(u => u.TenDangNhap == model.TenDangNhap))
            {
                errors.Add("tenDangNhap", "Tên đăng nhập đã tồn tại!");
            }

            // Kiểm tra trùng email
            if (_context.TaiKhoan.Any(u => u.Email == model.Email))
            {
                errors.Add("email", "Email đã tồn tại!");
            }

            // Kiểm tra trùng số điện thoại
            if (_context.TaiKhoan.Any(u => u.SoDienThoai == model.SoDienThoai))
            {
                errors.Add("soDienThoai", "Số điện thoại đã tồn tại!");
            }

            // Nếu có bất kỳ lỗi nào, trả về tất cả các lỗi
            if (errors.Any())
            {
                return Json(new { success = false, errors = errors });
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
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/avatars", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        AnhDaiDien.CopyTo(stream);
                    }
                    model.AnhDaiDien = "/images/avatars/" + fileName;
                }

                // Chuẩn hóa dữ liệu trước khi lưu
                model.TenDangNhap = StringHelper.NormalizeString(model.TenDangNhap);
                model.HoTen = StringHelper.NormalizeString(model.HoTen);
                model.Email = StringHelper.NormalizeString(model.Email);
                model.SoDienThoai = StringHelper.NormalizeString(model.SoDienThoai);

                _context.TaiKhoan.Add(model);
                _context.SaveChanges();

                // Gửi email thông báo tài khoản mới
                try
                {
                    await _emailService.SendNewAccountNotificationAsync(
                        model.Email, 
                        model.TenDangNhap, 
                        model.MatKhau, 
                        model.HoTen
                    );
                }
                catch (Exception ex)
                {
                    // Log lỗi gửi email nhưng không ảnh hưởng đến việc tạo tài khoản
                    // Có thể thêm logging ở đây
                }

                return Json(new { success = true });
            }
            else
            {
                // Thêm các lỗi validation từ ModelState
                var modelErrors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key.Substring(0, 1).ToLower() + kvp.Key.Substring(1), // Chuyển key về camelCase
                        kvp => kvp.Value.Errors.First().ErrorMessage
                    );

                return Json(new { success = false, errors = modelErrors });
            }
        }


        [HttpPost]
        public IActionResult EditUserAjax(TaiKhoanModels model, IFormFile AnhDaiDien, int page = 1)
        {
            // Bỏ validate field file nếu không bắt buộc
            ModelState.Remove("AnhDaiDien");

            if (string.IsNullOrEmpty(model.TrangThai))
                model.TrangThai = "HoatDong";

            // Dictionary chứa tất cả các lỗi
            var errors = new Dictionary<string, string>();

            // Kiểm tra trùng tên đăng nhập (trừ chính user đang sửa)
            if (_context.TaiKhoan.Any(u => u.TenDangNhap == model.TenDangNhap && u.IdTaiKhoan != model.IdTaiKhoan))
            {
                errors.Add("tenDangNhap", "Tên đăng nhập đã tồn tại!");
            }

            // Kiểm tra trùng email
            if (_context.TaiKhoan.Any(u => u.Email == model.Email && u.IdTaiKhoan != model.IdTaiKhoan))
            {
                errors.Add("email", "Email đã tồn tại!");
            }

            // Kiểm tra trùng số điện thoại
            if (_context.TaiKhoan.Any(u => u.SoDienThoai == model.SoDienThoai && u.IdTaiKhoan != model.IdTaiKhoan))
            {
                errors.Add("soDienThoai", "Số điện thoại đã tồn tại!");
            }

            // Nếu có bất kỳ lỗi nào, trả về tất cả các lỗi
            if (errors.Any())
            {
                return Json(new { success = false, errors = errors });
            }

            if (ModelState.IsValid)
            {
                var user = _context.TaiKhoan.FirstOrDefault(u => u.IdTaiKhoan == model.IdTaiKhoan);
                if (user == null)
                {
                    errors.Add("general", "Người dùng không tồn tại!");
                    return Json(new { success = false, errors = errors });
                }

                // Cập nhật thông tin
                user.TenDangNhap = StringHelper.NormalizeString(model.TenDangNhap);
                user.MatKhau = model.MatKhau;
                user.HoTen = StringHelper.NormalizeString(model.HoTen);
                user.VaiTro = model.VaiTro;
                user.Email = StringHelper.NormalizeString(model.Email);
                user.SoDienThoai = StringHelper.NormalizeString(model.SoDienThoai);
                user.TrangThai = model.TrangThai?.Trim();
                user.NgayCapNhat = DateTime.Now;

                // Xử lý lưu file ảnh nếu có
                if (AnhDaiDien != null && AnhDaiDien.Length > 0)
                {
                    var fileName = Path.GetFileName(AnhDaiDien.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/avatars", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        AnhDaiDien.CopyTo(stream);
                    }
                    user.AnhDaiDien = "/images/avatars/" + fileName;
                }

                _context.TaiKhoan.Update(user);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            else
            {
                // Thêm các lỗi validation từ ModelState
                var modelErrors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key.Substring(0, 1).ToLower() + kvp.Key.Substring(1), // Chuyển key về camelCase
                        kvp => kvp.Value.Errors.First().ErrorMessage
                    );

                return Json(new { success = false, errors = modelErrors });
            }
        }
        

        [HttpGet]
        public IActionResult EditUserPartial(int id)
        {
            var user = _context.TaiKhoan.FirstOrDefault(u => u.IdTaiKhoan == id);
            if (user == null)
                return Content("Không tìm thấy người dùng");

            if (user.TrangThai == "KhongHoatDong")
            {
                return Content("Không thể chỉnh sửa người dùng đang ở trạng thái Không hoạt động!", "text/plain; charset=utf-8");
            }

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
            return PartialView("~/Views/Shared/_PaginationPartial.cshtml");
        }

        // Download Excel template
        [HttpGet]
        public IActionResult DownloadExcelTemplate()
        {
            var excelBytes = _excelService.GenerateExcelTemplate();
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "UserImportTemplate.xlsx");
        }

        // Preview Excel import
        [HttpPost]
        public IActionResult PreviewExcelImport(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn file Excel" });
            }

            try
            {
                using (var stream = excelFile.OpenReadStream())
                {
                    var result = _excelService.ImportUsersFromExcel(stream);
                    return Json(new { 
                        success = true, 
                        data = result,
                        message = $"Đã đọc {result.TotalRows} dòng, {result.ValidRows} dòng hợp lệ, {result.ErrorRows} dòng có lỗi"
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi đọc file: {ex.Message}" });
            }
        }

        // Confirm and import users
        [HttpPost]
        public async Task<IActionResult> ConfirmImportUsers([FromBody] List<ExcelService.ExcelUserData> users)
        {
            if (users == null || !users.Any())
            {
                return Json(new { success = false, message = "Không có dữ liệu để import" });
            }

            var errors = new List<string>();
            var successCount = 0;

            foreach (var userData in users)
            {
                try
                {
                    // Kiểm tra trùng lặp
                    if (_context.TaiKhoan.Any(u => u.TenDangNhap == userData.TenDangNhap))
                    {
                        errors.Add($"Tên đăng nhập '{userData.TenDangNhap}' đã tồn tại");
                        continue;
                    }

                    if (_context.TaiKhoan.Any(u => u.Email == userData.Email))
                    {
                        errors.Add($"Email '{userData.Email}' đã tồn tại");
                        continue;
                    }

                    if (_context.TaiKhoan.Any(u => u.SoDienThoai == userData.SoDienThoai))
                    {
                        errors.Add($"Số điện thoại '{userData.SoDienThoai}' đã tồn tại");
                        continue;
                    }

                    // Tạo user mới
                    var newUser = new TaiKhoanModels
                    {
                        TenDangNhap = StringHelper.NormalizeString(userData.TenDangNhap),
                        MatKhau = userData.MatKhau,
                        HoTen = StringHelper.NormalizeString(userData.HoTen),
                        VaiTro = userData.VaiTro,
                        Email = StringHelper.NormalizeString(userData.Email),
                        SoDienThoai = StringHelper.NormalizeString(userData.SoDienThoai),
                        TrangThai = "HoatDong",
                        NgayTao = DateTime.Now,
                        NgayCapNhat = DateTime.Now
                    };

                    _context.TaiKhoan.Add(newUser);
                    successCount++;

                    // Gửi email thông báo tài khoản mới
                    try
                    {
                        await _emailService.SendNewAccountNotificationAsync(
                            newUser.Email, 
                            newUser.TenDangNhap, 
                            newUser.MatKhau, 
                            newUser.HoTen
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi gửi email nhưng không ảnh hưởng đến việc import
                        errors.Add($"Đã tạo tài khoản '{userData.TenDangNhap}' nhưng không gửi được email thông báo");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Lỗi khi tạo user '{userData.TenDangNhap}': {ex.Message}");
                }
            }

            if (successCount > 0)
            {
                _context.SaveChanges();
            }

            return Json(new { 
                success = true, 
                message = $"Import thành công {successCount} người dùng",
                errors = errors,
                successCount = successCount
            });
        }
    }
} 