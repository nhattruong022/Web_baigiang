using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Lecture_web.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Giangvien,Sinhvien")]
    public class LopHocController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LopHocController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            ViewBag.UserRole = userRole;

            try
            {
                // Lấy danh sách lớp học phần từ database với filter theo user role
                var lopHocPhanQuery = _context.LopHocPhan
                    .Include(lhp => lhp.HocPhan)
                    .Include(lhp => lhp.TaiKhoan)
                    .Include(lhp => lhp.LopHocPhan_SinhViens)
                        .ThenInclude(sv => sv.TaiKhoan)
                    .AsQueryable();

                // Nếu là sinh viên, chỉ hiển thị các lớp mà sinh viên đã tham gia
                if (userRole == "Sinhvien")
                {
                    lopHocPhanQuery = lopHocPhanQuery
                        .Where(lhp => lhp.LopHocPhan_SinhViens.Any(sv => sv.IdTaiKhoan == currentUserId));
                }
                // Nếu là giảng viên, hiển thị các lớp do giảng viên tạo
                else if (userRole == "Giangvien")
                {
                    lopHocPhanQuery = lopHocPhanQuery
                        .Where(lhp => lhp.IdTaiKhoan == currentUserId);
                }

                var lopHocPhans = await lopHocPhanQuery
                    .Select(lhp => new
                    {
                        lhp.IdLopHocPhan,
                        lhp.TenLop,
                        lhp.MoTa,
                        lhp.AnhDaiDien,
                        lhp.TrangThai,
                        lhp.NgayTao,
                        lhp.IdTaiKhoan,
                        lhp.IdHocPhan,
                        TenHocPhan = lhp.HocPhan.TenHocPhan ?? "N/A",
                        TenGiangVien = lhp.TaiKhoan.HoTen ?? "N/A",
                        AnhDaiDienGV = lhp.TaiKhoan.AnhDaiDien,
                        SoSinhVien = lhp.LopHocPhan_SinhViens.Count(sv => sv.TaiKhoan.VaiTro == "Sinhvien")
                    })
                    .OrderByDescending(lhp => lhp.NgayTao)
                    .ToListAsync();

                ViewBag.LopHocPhans = lopHocPhans;
                ViewBag.CurrentUserId = currentUserId;

                return View();
            }
            catch (Exception ex)
            {
                // Log error without console spam
                ViewBag.LopHocPhans = new List<object>();
                ViewBag.CurrentUserId = currentUserId;
                return View();
            }
        }
    }
} 


