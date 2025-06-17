using Lecture_web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lecture_web.Areas.User.Controllers
{
    [Area("User")]
    public class QuanLyChuongController : Controller
    {
        private readonly ApplicationDbContext _context;
        public QuanLyChuongController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index( string tenbg,string search, int page = 1)
        {
            const int pageSize = 5;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));


            var bg = await _context.BaiGiang
                .Where(b => b.IdTaiKhoan == userId)
                .Select(b => new {b.TieuDe })
                .Distinct()
                .ToListAsync();
            ViewBag.Lectures = bg;

           
            var q = _context.BaiGiang
                .Where(b => b.IdTaiKhoan == userId)
                .SelectMany(b => b.Chuongs, (b, c) => new { b, c })
                .Select(x => new ListChuongViewModel
                {
                    IdChuong = x.c.IdChuong,
                    Ten = x.c.TenChuong,
                    BaiGiang = x.b.TieuDe,
                    NgayTao = x.c.NgayTao,
                    NgayCapNhat = x.c.NgayCapNhat
                });

            if (!string.IsNullOrWhiteSpace(tenbg))
                q = q.Where(x => x.BaiGiang == tenbg);


            if (!string.IsNullOrWhiteSpace(search))
            {
                var key = search.Replace(" ", "").ToLower();
                q = q.Where(x =>
                    x.Ten.Replace(" ", "").ToLower().Contains(key)
                );
            }
            var totalItems = await q.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var items = await q
                .OrderByDescending(x => x.NgayCapNhat)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

  
            var listch = new SearchChuongViewModel<ListChuongViewModel>
            {
                Items = items,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchTerm = search,
                baigiang = tenbg
            };
            return View(listch);
        }
        [HttpGet]
        public async Task<IActionResult> EditChuong(int id)
        {
            var chuong = await _context.Chuong
                .Include(c => c.BaiGiang)
                .FirstOrDefaultAsync(c => c.IdChuong == id);
            if (chuong == null) return NotFound();

            return Json(new
            {
                idChuong = chuong.IdChuong,
                tenChuong = chuong.TenChuong,
                baiGiang = chuong.BaiGiang.TieuDe,
                listbg = await _context.BaiGiang
                              .Where(b => b.IdTaiKhoan == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)))
                              .Select(b => new { b.TieuDe })
                              .ToListAsync()
            });
        }


        [HttpPost]
        public async Task<IActionResult> EditChuong(EditChuongViewModel ch)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(m => m.Value.Errors.Any())
                    .ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { errors });
            }

            var chuong = await _context.Chuong
                .Include(c => c.BaiGiang)
                .FirstOrDefaultAsync(c => c.IdChuong == ch.idchuong);
            if (chuong == null) return NotFound();

            // Tìm bài giảng mới
            var bai = await _context.BaiGiang
                .FirstOrDefaultAsync(b => b.TieuDe == ch.BaiGiang);
            if (bai == null)
            {
                ModelState.AddModelError(nameof(ch.BaiGiang), "Bài giảng không tồn tại.");
                var errors = ModelState
                    .Where(m => m.Value.Errors.Any())
                    .ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { errors });
            }

            // 1) Kiểm tra duplicate trong cùng bài giảng
            bool duplicate = await _context.Chuong
                .Where(c => c.IdBaiGiang == bai.IdBaiGiang
                         && c.TenChuong == ch.tenchuong
                         && c.IdChuong != ch.idchuong)
                .AnyAsync();
            if (duplicate)
            {
                ModelState.AddModelError(
                    nameof(ch.tenchuong),
                    "Tên chương này đã tồn tại trong bài giảng đã chọn."
                );
                var errors = ModelState
                    .Where(m => m.Value.Errors.Any())
                    .ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { errors });
            }

            // 2) Gán lại và lưu
            chuong.TenChuong = ch.tenchuong;
            chuong.IdBaiGiang = bai.IdBaiGiang;
            chuong.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }


    }
} 