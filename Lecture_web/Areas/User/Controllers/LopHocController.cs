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
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index(string search, int page = 1, int? idHocPhan = null)
        {
            const int pageSize = 6;
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
                    Mota = lp.MoTa,
                    IdHocPhan = lp.IdHocPhan
                });

            if (idHocPhan.HasValue)
            {
                getLop = getLop.Where(x => x.IdHocPhan == idHocPhan.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var key = search.Trim().Replace(" ", "").ToLower();
                getLop = getLop.Where(x =>
                    (x.TenHP + x.TenLop).Trim()
                     .Replace(" ", "")
                     .ToLower()
                     .Contains(key)
                );
            }

            var total = await getLop.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPages == 0)
            {
                page = 1;
            }
            else
            {
                if (page < 1 || page > totalPages)
                    return NotFound();
            }
            var list = await getLop
                             .OrderByDescending(x => x.NgayTao)
                             .Skip((page - 1) * pageSize)
                             .Take(pageSize)
                             .ToListAsync();

            if (idHocPhan.HasValue && !list.Any())
            {
                string message = role == "Sinhvien" ? "Chưa tham gia lớp học này" : "Không sở hữu lớp này";
                ViewBag.AccessDeniedMessage = message;
                return View("AccessDenied");
            }

            var gvIds = list.Select(x => x.GiangVienId).Distinct().ToList();
            var gvinfo = await _context.TaiKhoan
                              .Where(u => gvIds.Contains(u.IdTaiKhoan))
                              .Select(u => new { u.IdTaiKhoan, u.HoTen, u.AnhDaiDien })
                              .ToListAsync();

            var vmItems = list.Select(x =>
            {
                var gv = gvinfo.First(u => u.IdTaiKhoan == x.GiangVienId);
                
                // Xử lý avatar URL với fallback
                string avatarUrl = null;
                if (!string.IsNullOrEmpty(gv.AnhDaiDien))
                {
                    // Nếu đường dẫn đã có / ở đầu thì dùng trực tiếp
                    if (gv.AnhDaiDien.StartsWith("/"))
                    {
                        avatarUrl = gv.AnhDaiDien;
                    }
                    // Nếu đường dẫn bắt đầu bằng images/ thì thêm / ở đầu
                    else if (gv.AnhDaiDien.StartsWith("images/"))
                    {
                        avatarUrl = "/" + gv.AnhDaiDien;
                    }
                    // Nếu không có format chuẩn thì thêm prefix
                    else
                    {
                        avatarUrl = "/images/avatars/" + gv.AnhDaiDien;
                    }
                }
                // Fallback nếu không có avatar
                else
                {
                    avatarUrl = "/images/avatars/avatar.jpg";
                }
                
                return new LopHocViewModel
                {
                    IdLopHocPhan = x.IdLopHocPhan,
                    TenLop = x.TenLop,
                    TenHocPhan = x.TenHP,
                    SoSinhVien = x.SoSV,
                    NgayTao = x.NgayTao,
                    NgayCapNhat = x.NgayCapNhat,
                    GiangVienName = gv.HoTen,
                    GiangVienAvatarUrl = avatarUrl,
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

    
            var lop = new LopHocPhanModels
            {
                TenLop = lhp.TenLop,
                MoTa = lhp.MoTa,
                IdHocPhan = lhp.HocPhanId.Value,
                IdBaiGiang = lhp.BaiGiangId,        
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
       
           lhp.TenLop = StringHelper.NormalizeString(lhp.TenLop);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        
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

            // Lấy ra lớp đang sửa
            var lop = await _context.LopHocPhan
                .FirstOrDefaultAsync(lp =>
                    lp.IdLopHocPhan ==lhp.IdLopHocPhan &&
                    lp.IdTaiKhoan == userId
                );
            if (lop == null)
                return NotFound();

            
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
            lop.IdHocPhan =lhp.HocPhanId.Value;
            lop.IdBaiGiang =lhp.BaiGiangId;
            lop.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>DeleteClass(int  idLopHocPhan)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

       
            var lop = await _context.LopHocPhan
                .FirstOrDefaultAsync(lp =>
                    lp.IdLopHocPhan == idLopHocPhan &&
                    lp.IdTaiKhoan == userId
                );
            if (lop == null)
                return NotFound(new { error = "Lớp không tồn tại hoặc bạn không có quyền xóa." });

            if (lop.IdBaiGiang.HasValue)
                return BadRequest(new { error = "Không thể xóa vì lớp đang liên kết với bài giảng." });

    
            _context.LopHocPhan.Remove(lop);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OutClass(int idLop)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var rootIds = await _context.BinhLuan
                .Where(bl => bl.IdLopHocPhan == idLop &&
                             bl.IdTaiKhoan == userId)
                .Select(bl => bl.IdBinhLuan)
                .ToListAsync();


            if (rootIds.Any())
            {
               await _context.Database.ExecuteSqlRawAsync(
                            @"UPDATE dbo.BinhLuan
                  SET IdBinhLuanCha = NULL
                  WHERE IdBinhLuanCha IN ({0});",
                            string.Join(",", rootIds)
                        );
            }
            // Xóa tất cả bl không có bl cha
            await _context.BinhLuan
                .Where(bl => rootIds.Contains(bl.IdBinhLuan))
                .ExecuteDeleteAsync();

            // Xóa bình luận sinh viên sau khi rời lớp
            await _context.BinhLuan
                .Where(bl => bl.IdLopHocPhan == idLop &&
                             bl.IdTaiKhoan == userId)
                .ExecuteDeleteAsync();

            await _context.LopHocPhan_SinhVien
            .Where(ls => ls.IdLopHocPhan == idLop &&
                         ls.IdTaiKhoan == userId)
            .ExecuteDeleteAsync();


            return Json(new { success = true });
        }

    }


} 



