using Lecture_web.Models.ViewModels;
using Lecture_web.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Lecture_web.Models;

namespace Lecture_web.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Giangvien")]

    public class QuanLyChuongController : Controller
    {
        private readonly ApplicationDbContext _context;
        public QuanLyChuongController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index( int idbg, string tenbg,string search, int page = 1)
        {
            const int pageSize = 5;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));


            //var bg = await _context.BaiGiang
            //                .Where(b => b.IdTaiKhoan == userId)
            //                .Select(b => new {b.TieuDe })
            //                .Distinct()
            //                .ToListAsync();
            //ViewBag.Lectures = bg;


            var tbg = await _context.BaiGiang
                            .Where(b => b.IdBaiGiang == idbg && b.IdTaiKhoan == userId)
                            .Select(b => b.TieuDe)
                            .FirstOrDefaultAsync();

            if (tbg == null)
            {
                return Forbid();
            }

            var q = _context.BaiGiang
                            .Where(b => b.IdTaiKhoan == userId && b.IdBaiGiang == idbg)
                            .SelectMany(b => b.Chuongs, (b, c) => new { b, c })
                            .Select(x => new ListChuongViewModel
                            {
                                IdChuong = x.c.IdChuong,
                                Ten = x.c.TenChuong,
                                BaiGiang = x.b.TieuDe,
                                NgayTao = x.c.NgayTao,
                                NgayCapNhat = x.c.NgayCapNhat,
                                Bai = _context.Bai
                                 .Where(l => l.IdChuong == x.c.IdChuong)
                                 .Select(l => new BaiViewModel
                                 {
                                     IdBai = l.IdBai,
                                     IdChuong = l.IdChuong,
                                     TenBai = l.TieuDeBai,
                                     NoiDung = l.NoiDungText,
                                     NgayTao = l.NgayTao,
                                     NgayCapNhat = l.NgayCapNhat
                                 }).ToList()
                            });

 

            //if (!string.IsNullOrWhiteSpace(tenbg))
            //    q = q.Where(x => x.BaiGiang == tenbg);


            if (!string.IsNullOrWhiteSpace(search))
            {
                var key = search.Replace(" ", "").Trim().Replace(" ","").ToLower();
                q = q.Where(x =>
                    x.Ten.Replace(" ", "").Trim().ToLower().Contains(key)
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
                            .OrderByDescending(x => x.NgayTao)
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync();

  
            var listch = new SearchChuongViewModel<ListChuongViewModel>
            {
                Items = items,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchTerm = search,
                baigiang = tenbg,
                IdBaiGiang = idbg,
                TenBaiGiang = tbg

            };
            return View(listch);
        }
        //[HttpGet]
        //public async Task<IActionResult> EditChuong(int id)
        //{
        //    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        //    var chuong = await _context.Chuong
        //        .Include(c => c.BaiGiang)
        //        .FirstOrDefaultAsync(c => c.IdChuong == id);
        //    if (chuong == null) return NotFound();

        //    if (chuong.BaiGiang.IdTaiKhoan != userId)
        //    {
        //        return NotFound();
        //    }

        //    return Json(new
        //    {
        //        idChuong = chuong.IdChuong,
        //        tenChuong = StringHelper.NormalizeString(chuong.TenChuong),
        //        baiGiang = chuong.BaiGiang.TieuDe,
        //        listbg = await _context.BaiGiang
        //            .Where(b => b.IdTaiKhoan == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)))
        //            .Select(b => new { b.TieuDe })
        //            .ToListAsync()
        //    });
        //}


        //[HttpPost]
        //public async Task<IActionResult> EditChuong(EditChuongViewModel ch)
        //{
        //    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        //    var td = StringHelper.NormalizeString(ch.tenchuong);
        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState
        //            .Where(m => m.Value.Errors.Any())
        //            .ToDictionary(
        //                kv => kv.Key,
        //                kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
        //            );
        //        return BadRequest(new { errors });
        //    }

        //    var chuong = await _context.Chuong
        //        .Include(c => c.BaiGiang)
        //        .FirstOrDefaultAsync(c => c.IdChuong == ch.idchuong);
        //    if (chuong == null) return NotFound();

        //    // Tìm bài giảng mới
        //    var bai = await _context.BaiGiang
        //        .FirstOrDefaultAsync(b => b.TieuDe == ch.BaiGiang);
        //    if (bai == null)
        //    {
        //        ModelState.AddModelError(nameof(ch.BaiGiang), "Bài giảng không tồn tại.");
        //        var errors = ModelState
        //            .Where(m => m.Value.Errors.Any())
        //            .ToDictionary(
        //                kv => kv.Key,
        //                kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
        //            );
        //        return BadRequest(new { errors });
        //    }
        //    var exist = await _context.Chuong
        //           .Where(c =>
        //               c.IdBaiGiang == bai.IdBaiGiang
        //            && c.IdChuong != ch.idchuong
        //           )
        //           .Select(c => c.TenChuong)
        //           .ToListAsync();


        //    bool check = exist
        //        .Select(n => StringHelper.NormalizeString(n))
        //        .Any(n =>
        //            string.Equals(n, td, StringComparison.OrdinalIgnoreCase)
        //        );

        //    if (check)
        //    {
        //        ModelState.AddModelError(nameof(ch.tenchuong),
        //            "Tên chương này đã tồn tại trong bài giảng đã chọn.");
        //        var errors = ModelState
        //            .Where(m => m.Value.Errors.Any())
        //            .ToDictionary(
        //                kv => kv.Key,
        //                kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
        //            );
        //        return BadRequest(new { errors });
        //    }

        //    chuong.TenChuong = td;
        //    chuong.IdBaiGiang = bai.IdBaiGiang;
        //    chuong.NgayCapNhat = DateTime.Now;

        //    await _context.SaveChangesAsync();
        //    return Ok(new { success = true });
        //}

        //[HttpGet]
        //public async Task<IActionResult> CreateChuong()
        //{
        //    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        //    var lectures = await _context.BaiGiang
        //        .Where(b => b.IdTaiKhoan == userId)
        //        .Select(b => b.TieuDe)
        //        .ToListAsync();
        //    return Json(new { listbg = lectures });
        //}

        //[HttpPost]
        //public async Task<IActionResult> CreateChuong(EditChuongViewModel ch)
        //{

        //    ch.tenchuong = StringHelper.NormalizeString(ch.tenchuong);
        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState
        //            .Where(m => m.Value.Errors.Any())
        //            .ToDictionary(
        //                kv => kv.Key,
        //                kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
        //            );
        //        return BadRequest(new { errors });
        //    }

        //    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        //    var bai = await _context.BaiGiang
        //        .FirstOrDefaultAsync(b => b.TieuDe == ch.BaiGiang && b.IdTaiKhoan == userId);
        //    if (bai == null)
        //    {
        //        ModelState.AddModelError(nameof(ch.BaiGiang), "Bài giảng không tồn tại");
        //        var errors = ModelState
        //            .Where(m => m.Value.Errors.Any())
        //            .ToDictionary(
        //                kv => kv.Key,
        //                kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
        //            );
        //        return BadRequest(new { errors });
        //    }

        //        var otherTitles = await _context.Chuong
        //            .Where(c => c.IdBaiGiang == bai.IdBaiGiang)
        //            .Select(c => c.TenChuong)
        //            .ToListAsync();

        //        bool duplicate = otherTitles
        //            .Select(t => StringHelper.NormalizeString(t))
        //            .Any(normalized =>
        //                string.Equals(normalized,
        //                                ch.tenchuong,
        //                                StringComparison.OrdinalIgnoreCase));

        //        if (duplicate)
        //        {
        //            ModelState.AddModelError(nameof(ch.tenchuong),
        //                "Tên chương này đã tồn tại trong bài giảng đã chọn.");
        //            var errors = ModelState
        //                .Where(k => k.Value.Errors.Any())
        //                .ToDictionary(
        //                    kv => kv.Key,
        //                    kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
        //                );
        //            return BadRequest(new { errors });
        //        }




        //    var chuong = new Models.ChuongModels
        //    {
        //        TenChuong = ch.tenchuong,
        //        IdBaiGiang = bai.IdBaiGiang,
        //        NgayTao = DateTime.Now,
        //        NgayCapNhat = DateTime.Now
        //    };
        //    _context.Chuong.Add(chuong);
        //    await _context.SaveChangesAsync();
        //    return Ok(new { success = true });
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateChuong(int idBaiGiang, string tenchuong)
        {
            if (string.IsNullOrWhiteSpace(tenchuong))
            {
                return BadRequest(new
                {
                    errors = new
                    {
                        tenchuong = string.IsNullOrWhiteSpace(tenchuong) ? new[] { "Tên chương không được để trống." } : null
                    }
                });
            }
            if (tenchuong.Length > 100)
            {
                return BadRequest(new
                {
                    errors = new
                    {
                        tenchuong = new[] { "Tên chương không được vượt quá 100 ký tự." }
                    }
                });
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var bai = await _context.BaiGiang
                            .FirstOrDefaultAsync(b => b.IdBaiGiang == idBaiGiang && b.IdTaiKhoan == userId);
            if (bai == null)
            {
                return BadRequest(new
                {
                    errors = new
                    {
                        IdBaiGiang = new[] { "Bài giảng không tồn tại hoặc không thuộc quyền bạn." }
                    }
                });
            }

            var addChuongExist = tenchuong.Trim().Replace(" ","").ToLowerInvariant();
            bool exists = await _context.Chuong
                .AnyAsync(c => c.IdBaiGiang == idBaiGiang
                            && c.TenChuong.Trim().Replace(" ","").ToLower() == addChuongExist);
            if (exists)
            {
                return BadRequest(new
                {
                    errors = new
                    {
                        tenchuong = new[] { "Tên chương đã tồn tại." }
                    }
                });
            }

            var chuong = new ChuongModels
            {
                TenChuong = tenchuong.Trim(),
                IdBaiGiang = idBaiGiang,
                NgayTao = DateTime.Now,
                NgayCapNhat = DateTime.Now
            };
            _context.Chuong.Add(chuong);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, newId = chuong.IdChuong });
        }

        [HttpGet]
        public async Task<IActionResult> EditChuong(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var chuong = await _context.Chuong
                .Include(c => c.BaiGiang)
                .FirstOrDefaultAsync(c => c.IdChuong == id && c.BaiGiang.IdTaiKhoan == userId);
            if (chuong == null) return BadRequest();

            var listbg = await _context.BaiGiang
                .Where(b => b.IdTaiKhoan == userId)
                .Select(b => new { b.IdBaiGiang, b.TieuDe })
                .ToListAsync();

            return Json(new
            {
                idChuong = chuong.IdChuong,
                tenChuong = chuong.TenChuong,
                baiGiang = chuong.BaiGiang.TieuDe,
                listbg = listbg.Select(b => new { tieuDe = b.TieuDe, id = b.IdBaiGiang })
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditChuong(int idchuong, string tenchuong)
        {
            if (string.IsNullOrWhiteSpace(tenchuong))
            {
                return BadRequest(new
                {
                    errors = new
                    {
                        tenchuong = string.IsNullOrWhiteSpace(tenchuong) ? new[] { "Tên chương không được để trống." } : null
                    }
                });
            }
            if (tenchuong.Length > 100)
            {
                return BadRequest(new
                {
                    errors = new
                    {
                        tenchuong = new[] { "Tên chương không được vượt quá 100 ký tự." }
                    }
                });
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var chuong = await _context.Chuong
                .Include(c => c.BaiGiang)
                .FirstOrDefaultAsync(c =>
                    c.IdChuong == idchuong &&
                    c.BaiGiang.IdTaiKhoan == userId);

            if (chuong == null)
                return BadRequest(new { error = "Chương không tồn tại hoặc bạn không có quyền." });

            var editExistChuong = tenchuong?.Trim().Replace(" ","").ToLowerInvariant();
            bool exists = await _context.Chuong.AnyAsync(c =>
                c.IdChuong != idchuong &&
                c.IdBaiGiang == chuong.IdBaiGiang &&
                c.TenChuong.Trim().Replace(" ", "").ToLower() == editExistChuong);

            if (exists)
                ModelState.AddModelError("tenchuong", "Tên chương đã tồn tại.");

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    errors = ModelState
                        .Where(kv => kv.Value.Errors.Any())
                        .ToDictionary(
                            kv => kv.Key,
                            kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        )
                });
            }

            chuong.TenChuong = tenchuong.Trim();
            chuong.NgayCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChuong(int idchuong)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var chuong = await _context.Chuong
                .Include(c => c.BaiGiang)
                .FirstOrDefaultAsync(c => c.IdChuong == idchuong && c.BaiGiang.IdTaiKhoan == userId);
            if (chuong == null)
                return NotFound();
            bool hasLessons = await _context.Bai
                .AnyAsync(bh => bh.IdChuong == idchuong);
            if (hasLessons)
                return BadRequest(new { error = "Chương đang có bài học liên kết, không thể xóa." });
            _context.Chuong.Remove(chuong);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MultipleDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest(new { message = "Chưa chọn bài nào để xóa." });
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var bai = await _context.Bai
                .Include(b => b.Chuong)                           
                .ThenInclude(c => c.BaiGiang)                    
                .Where(b => ids.Contains(b.IdBai)
                            && b.Chuong.BaiGiang.IdTaiKhoan == userId)
                .ToListAsync();

            if (!bai.Any())
            {
                return BadRequest(new { message = "Không tìm thấy bài hợp lệ để xóa." });
            }

            _context.Bai.RemoveRange(bai);
            await _context.SaveChangesAsync();

            return Ok(new { deletedCount = bai.Count });
        }


    }
} 