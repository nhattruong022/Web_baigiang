using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Lecture_web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Lecture_web.Models;
using Lecture_web.Service;

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
        [HttpGet]
        public async Task<IActionResult> Index(string search, int page = 1)
        {
            const int pageSize = 4;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var role = User.FindFirst(ClaimTypes.Role)?.Value;


            var typeUser =
                role == "Giangvien"
                  ? _context.LopHocPhan.Where(lp => lp.IdTaiKhoan == userId)
                  : _context.LopHocPhan
                      .Where(lp => lp.LopHocPhan_SinhViens.Any(sv => sv.IdTaiKhoan == userId));


                         var getLop = typeUser
                .Include(lp => lp.HocPhan)
                .Include(lp => lp.LopHocPhan_SinhViens)
                    .ThenInclude(sv => sv.TaiKhoan)
                .Select(lp => new
                {
                    lp.IdLopHocPhan,
                    lp.TenLop,
                    TenHP = lp.HocPhan.TenHocPhan,
                    lp.NgayTao,
                    lp.NgayCapNhat,
 
                    SoSV = lp.LopHocPhan_SinhViens.Count(sv => 
                        sv.TaiKhoan != null && sv.TaiKhoan.VaiTro == "Sinhvien"),
                    GiangVienId = lp.IdTaiKhoan,
                    Mota = lp.MoTa
                });


            if (!string.IsNullOrWhiteSpace(search))
            {
                var key = search.Replace(" ", "").ToLower();
                getLop = getLop.Where(x =>
                    (x.TenHP + x.TenLop)
                     .Replace(" ", "")
                     .ToLower()
                     .Contains(key)
                );
            }

            var total = await getLop.CountAsync();
            var list = await getLop
                             .OrderByDescending(x => x.NgayTao)
                             .Skip((page - 1) * pageSize)
                             .Take(pageSize)
                             .ToListAsync();

 
            var gvIds = list.Select(x => x.GiangVienId).Distinct().ToList();
            var gvinfo = await _context.TaiKhoan
                              .Where(u => gvIds.Contains(u.IdTaiKhoan))
                              .Select(u => new { u.IdTaiKhoan, u.HoTen, u.AnhDaiDien })
                              .ToListAsync();

            var vmItems = list.Select(x =>
            {
                var gv = gvinfo.First(u => u.IdTaiKhoan == x.GiangVienId);
                return new LopHocViewModel
                {
                    IdLopHocPhan = x.IdLopHocPhan,
                    TenLop = x.TenLop,
                    TenHocPhan = x.TenHP,
                    SoSinhVien = x.SoSV,
                    NgayTao = x.NgayTao,
                    NgayCapNhat = x.NgayCapNhat,
                    GiangVienName = gv.HoTen,
                    GiangVienAvatarUrl = gv.AnhDaiDien,
                    Mota = x.Mota
                };
            }).ToList();

            var lhp = new SearchBaiGiangViewModel<LopHocViewModel>
            {
                Items = vmItems,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                SearchTerm = search
            };

            ViewBag.UserRole = role;
            return View(lhp);
        }
        [HttpGet]
        public async Task<IActionResult> GetClass(int? id)
        {
            // load lists of HocPhan and BaiGiang for dropdowns
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var listBG = await _context.BaiGiang
                .Where(b => b.IdTaiKhoan == userId)
                .Select(b => new { b.IdBaiGiang, b.TieuDe })
                .ToListAsync();
            var listHP = await _context.HocPhan
                .Select(h => new { h.IdHocPhan, h.TenHocPhan })
                .ToListAsync();

            if (id == null)
                return Json(new { lbg = listBG, lhp = listHP });

            var getidlhp = await _context.LopHocPhan
                .Include(lp => lp.HocPhan)
                .FirstOrDefaultAsync(lp =>
                    lp.IdLopHocPhan == id &&
                    lp.IdTaiKhoan == userId
                 );
            if (getidlhp == null) return NotFound();

            return Json(new
            {
                id = getidlhp.IdLopHocPhan,
                ten = getidlhp.TenLop,
                mota = getidlhp.MoTa,
                hocPhanId = getidlhp.IdHocPhan,
                baiGiangId = getidlhp.IdBaiGiang,
                getlistbg = listBG,
                getlisthp = listHP
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass(AddClassViewModel lhp)
        {
            lhp.TenLop = StringHelper.NormalizeString(lhp.TenLop);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(k => k.Value.Errors.Any())
                    .ToDictionary(
                        k => k.Key,
                        k => k.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { errors });
            }

            // Kiểm tra học phần tồn tại
            var hocPhan = await _context.HocPhan
                .FirstOrDefaultAsync(h => h.IdHocPhan == lhp.HocPhanId);
            if (hocPhan == null)
            {
                ModelState.AddModelError(nameof(lhp.HocPhanId), "Học phần không tồn tại.");
                var errors = ModelState
                    .Where(k => k.Value.Errors.Any())
                    .ToDictionary(
                        k => k.Key,
                        k => k.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { errors });
            }

            // Kiểm tra trùng HọcPhan–TênLop
            lhp.TenLop = StringHelper.NormalizeString(lhp.TenLop);
            bool exists = await _context.LopHocPhan
                .AnyAsync(c => c.IdHocPhan == lhp.HocPhanId && c.TenLop.Replace(" ","") == lhp.TenLop.Replace(" ",""));
            if (exists)
            {
                ModelState.AddModelError(nameof(lhp.TenLop),
                    "Tên lớp này đã tồn tại trong học phần đã chọn.");
                var errors = ModelState
                    .Where(k => k.Value.Errors.Any())
                    .ToDictionary(
                        k => k.Key,
                        k => k.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { errors });
            }

            // Tạo mới
            var lop = new LopHocPhanModels
            {
                TenLop = lhp.TenLop,
                MoTa = lhp.MoTa,
                IdHocPhan = lhp.HocPhanId.Value,
                IdBaiGiang = lhp.BaiGiangId,      
                TrangThai = "Hoạt động",       
                NgayTao = DateTime.Now,
                NgayCapNhat = DateTime.Now,
                IdTaiKhoan = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
            };
            _context.LopHocPhan.Add(lop);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClass(EditClassViewModel lhp)
        {
            // 1) Chuẩn hóa tên
           lhp.TenLop = StringHelper.NormalizeString(lhp.TenLop);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // 2) Kiểm tra ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { errors });
            }

            // 3) Lấy lớp cần sửa
            var lop = await _context.LopHocPhan
                .FirstOrDefaultAsync(lp =>
                    lp.IdLopHocPhan ==lhp.IdLopHocPhan &&
                    lp.IdTaiKhoan == userId
                );
            if (lop == null)
                return NotFound();

            // 4) Kiểm tra học phần mới có tồn tại không
            var hocPhan = await _context.HocPhan
                .AnyAsync(h => h.IdHocPhan ==lhp.HocPhanId);
            if (!hocPhan)
            {
                ModelState.AddModelError(
                    nameof(lhp.HocPhanId),
                    "Học phần không tồn tại."
                );
                var errors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { errors });
            }

 
            bool exists = await _context.LopHocPhan
                .AnyAsync(x =>
                    x.IdHocPhan ==lhp.HocPhanId &&
                    x.TenLop.Replace(" ","") == lhp.TenLop.Replace(" ","") &&
                    x.IdLopHocPhan !=lhp.IdLopHocPhan
                );
            if (exists)
            {
                ModelState.AddModelError(
                    nameof(lhp.TenLop),
                    "Tên lớp này đã tồn tại trong học phần đã chọn."
                );
                var errors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { errors });
            }


            lop.TenLop =lhp.TenLop;
            lop.MoTa =lhp.MoTa;
            lop.IdHocPhan =lhp.HocPhanId;
            lop.IdBaiGiang =lhp.BaiGiangId;
            lop.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }


} 



