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

        public IActionResult AddBai()
        {
          return View();
        }

        public async Task<IActionResult> EditBai(int idbai, int idchuong)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var bai = await _context.Bai
                .Include(b => b.Chuong).ThenInclude(c => c.BaiGiang)
                .FirstOrDefaultAsync(b =>
                    b.IdBai == idbai &&
                    b.Chuong.BaiGiang.IdTaiKhoan == userId
                );
            if (bai == null) return NotFound();

      
            var vmbai = new EditBaiViewModel
            {
                IdBai = bai.IdBai,
                TieuDeBai = bai.TieuDeBai,
                NoiDung = bai.NoiDungText,
                IdChuong = idchuong
            };
            return View(vmbai);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBai(EditBaiViewModel vm)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var bai = await _context.Bai
                .Include(b => b.Chuong).ThenInclude(c => c.BaiGiang)
                .FirstOrDefaultAsync(b =>
                    b.IdBai == vm.IdBai &&
                    b.Chuong.BaiGiang.IdTaiKhoan == userId
                );
            if (bai == null) return NotFound();

            if (!ModelState.IsValid)
                return View(vm);


            bai.TieuDeBai = vm.TieuDeBai.Trim();
            bai.NoiDungText = vm.NoiDung;
            bai.NgayCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();

            var idbg = bai.Chuong.IdBaiGiang;
            return RedirectToAction("Index", "QuanLyChuong", new { area = "User", idbg = idbg });
        }

    }
} 