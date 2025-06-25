using Lecture_web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lecture_web.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Giangvien")]
    public class QuanLyBaiController : Controller
    {
        private readonly ApplicationDbContext _context;
        public QuanLyBaiController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> EditBai(int idbai, int idchuong)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var bai = await _context.Bai
                .Include(b => b.Chuong).ThenInclude(c => c.BaiGiang)
                .FirstOrDefaultAsync(b =>
                    b.IdBai == idbai &&
                    b.Chuong.BaiGiang.IdTaiKhoan == userId);
            if (bai == null) return NotFound();

            var vmb = new EditBaiViewModel
            {
                IdBai = bai.IdBai,
                TieuDeBai = bai.TieuDeBai,
                NoiDung = bai.NoiDungText,
                IdChuong = idchuong,
                IdBaiGiang = bai.Chuong.IdBaiGiang
            };
            return View(vmb);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBai(EditBaiViewModel vmb)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var bai = await _context.Bai
                .Include(b => b.Chuong).ThenInclude(c => c.BaiGiang)
                .FirstOrDefaultAsync(b =>
                    b.IdBai == vmb.IdBai &&
                    b.Chuong.BaiGiang.IdTaiKhoan == userId);
            if (bai == null) return NotFound();

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                  .Where(x => x.Value.Errors.Any())
                  .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                  );
                return BadRequest(new { errors });
            }

            bai.TieuDeBai = vmb.TieuDeBai.Trim();
            bai.NoiDungText = vmb.NoiDung;
            bai.NgayCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }



    }
} 