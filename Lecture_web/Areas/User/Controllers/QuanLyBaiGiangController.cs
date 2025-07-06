using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Lecture_web.Models.ViewModels;
using Microsoft.AspNetCore.Components.Forms;
using Lecture_web.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Lecture_web.Service;
using Microsoft.AspNetCore.Authorization;

namespace Lecture_web.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Giangvien")]
    public class QuanLyBaiGiangController : Controller
    {
        private readonly ApplicationDbContext _context;
        public QuanLyBaiGiangController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> Index(string search, int page = 1)
        {

            const int pageSize = 5;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));


            var q = _context.BaiGiang
                .Where(b => b.IdTaiKhoan == userId)
                .OrderByDescending(o=>o.NgayTao)
                .Include(b => b.LopHocPhans).ThenInclude(lp => lp.HocPhan)
                .Select(p => new BaiGiangViewModel
                {
                    IdBaiGiang = p.IdBaiGiang,
                    TieuDe = p.TieuDe,
                    MoTa = p.MoTa,
                    LopHocPhan = string.Join(", ",
                     p.LopHocPhans.Select(hp =>
                       hp.HocPhan.TenHocPhan + "-" + hp.TenLop)),
                    NgayTao = p.NgayTao,
                    NgayCapNhat = p.NgayCapNhat
                });


            if (!string.IsNullOrWhiteSpace(search))
            {
                var key = search.Trim().Replace(" ", "").ToLower();
                q = q.Where(x =>
                  x.TieuDe.Trim().Replace(" ", "")
                          .ToLower()
                          .Contains(key)
                );
            }

            var totalItems = await q.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0)
            {
                page = 1;
            }
            else
            {
                if (page < 1 || page > totalPages)
                    return NotFound();
            }
            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();


            var bg = new SearchBaiGiangViewModel<BaiGiangViewModel>
            {
                Items = items,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchTerm = search
            };

            return View(bg);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int idbg)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var bg = await _context.BaiGiang
          .Where(b => b.IdBaiGiang == idbg && b.IdTaiKhoan == userId)
          .Include(b => b.LopHocPhans)
          .ThenInclude(lp => lp.HocPhan)
          .FirstOrDefaultAsync();
            if (bg == null) return NotFound();

            var td = StringHelper.NormalizeString(bg.TieuDe);
            var mt = StringHelper.NormalizeString(bg.MoTa);

            var listSelected = bg.LopHocPhans.Select(lp => lp.IdLopHocPhan).ToList();


            var teacherClass = await _context.LopHocPhan
                .Where(lp => lp.IdTaiKhoan == userId)
                .Include(lp => lp.HocPhan)
                .Select(lp => new
                {
                    Id = lp.IdLopHocPhan,
                    Ten = lp.HocPhan.TenHocPhan + " - " + lp.TenLop
                })
                .ToListAsync();

            var disableClass = await _context.LopHocPhan
                .Where(x => x.IdBaiGiang.HasValue
                            && x.IdBaiGiang.Value != idbg)
                .Select(x => x.IdLopHocPhan)
                .Distinct()
                .ToListAsync();


            return Json(new
            {
                bg.IdBaiGiang,
                td,
                mt,
                Class = teacherClass,
                Selected = listSelected,
                Disable = disableClass
            });
        }
        [HttpPost]
        public async Task<IActionResult> LuuBaiGiang(LuuBaiGiangViewModel sbg)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));


            sbg.tieude = StringHelper.NormalizeString(sbg.tieude);

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kvp => kvp.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { errors });
            }

            if (await _context.BaiGiang.AnyAsync(b =>
               b.IdTaiKhoan == userId && b.TieuDe.Trim().Replace(" ","") == sbg.tieude.Trim().Replace(" ","") && b.IdBaiGiang != sbg.idbaigiang))
            {
                return BadRequest(new
                {
                    errors = new
                    {
                        tieude = new[] { "Tiêu đề đã tồn tại" }
                    }
                });
            }
            var bg = await _context.BaiGiang
                .FirstOrDefaultAsync(b =>
                    b.IdBaiGiang == sbg.idbaigiang &&
                    b.IdTaiKhoan == userId
                );
            if (bg == null) return NotFound();

            bg.TieuDe = StringHelper.NormalizeString(sbg.tieude);
            bg.MoTa = StringHelper.NormalizeString(sbg.mota);
            bg.NgayCapNhat = DateTime.Now;

            var allClasses = await _context.LopHocPhan
                .Where(lp => lp.IdTaiKhoan == userId)
                .ToListAsync();

            var selected = sbg.selectlophoc ?? new List<int>();
            foreach (var lp in allClasses)
            {
                if (selected.Contains(lp.IdLopHocPhan))
                    lp.IdBaiGiang = bg.IdBaiGiang;
                else if (lp.IdBaiGiang == bg.IdBaiGiang)
                    lp.IdBaiGiang = null;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetListClass()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));


            var teacherClass = await _context.LopHocPhan
                .Where(lp => lp.IdTaiKhoan == userId)
                .Include(lp => lp.HocPhan)
                .Select(lp => new
                {
                    Id = lp.IdLopHocPhan,
                    Ten = lp.HocPhan.TenHocPhan + " - " + lp.TenLop
                })
                .ToListAsync();


            var disableClass = await _context.LopHocPhan
                .Where(lp => lp.IdBaiGiang.HasValue)
                .Select(lp => lp.IdLopHocPhan)
                .Distinct()
                .ToListAsync();

            return Json(new
            {
                classes = teacherClass,
                disable = disableClass
            });
        }
        [HttpPost]
        public async Task<IActionResult> CreateBaiGiang(AddBaiGiangViewModel bg)
        {
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

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (await _context.BaiGiang.AnyAsync(b =>
                    b.IdTaiKhoan == userId && b.TieuDe.Trim().Replace(" ", "") == bg.tieude.Trim().Replace(" ","")))
            {
                return BadRequest(new
                {
                    errors = new
                    {
                        tieude = new[] { "Tiêu đề đã tồn tại" }
                    }
                });
            }


            var bai = new BaiGiangModels
            {
                TieuDe = StringHelper.NormalizeString(bg.tieude),
                MoTa = StringHelper.NormalizeString(bg.mota),
                NgayTao = DateTime.Now,
                NgayCapNhat = DateTime.Now,
                IdTaiKhoan = userId
            };
            _context.BaiGiang.Add(bai);
            await _context.SaveChangesAsync();

            var classes = await _context.LopHocPhan
                .Where(lp => bg.selectedlophocphanids.Contains(lp.IdLopHocPhan))
                .ToListAsync();
            foreach (var lp in classes)
                lp.IdBaiGiang = bai.IdBaiGiang;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBaiGiang(int idbg)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var bg = await _context.BaiGiang
                .FirstOrDefaultAsync(b => b.IdBaiGiang == idbg && b.IdTaiKhoan == userId);
            if (bg == null)
                return NotFound();
            bool existClass = await _context.LopHocPhan.AnyAsync(lp => lp.IdBaiGiang == idbg);
            if (existClass)
                return BadRequest(new { error = "Bài giảng đang có liên kết tới lớp học phần, không thể xóa." });
            bool existChuong = await _context.Chuong.AnyAsync(c => c.IdBaiGiang == idbg);
            if (existChuong)
                return BadRequest(new
                {
                    error = "Cần xóa hết chương và bài của bài giảng này trước khi xóa."
                });
            _context.BaiGiang.Remove(bg);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }


    }
}