using Lecture_web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Lecture_web.Service;
using Lecture_web.Models;
using DocumentFormat.OpenXml.Math;
using OpenXmlPowerTools;

namespace Lecture_web.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Giangvien")]
    public class QuanLyBaiController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ImageDataHandle _imgHandle;
        private readonly ConvertWordToHTML _convertWordToHTML;
        public QuanLyBaiController(ApplicationDbContext context, ImageDataHandle imgHandle, ConvertWordToHTML convertWordToHTML)
        {
            _context = context;
            _imgHandle = imgHandle;
            _convertWordToHTML = convertWordToHTML;
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

            [RequestSizeLimit(6_000_000)]
            [RequestFormLimits(ValueLengthLimit = int.MaxValue)]
            [HttpPost, ValidateAntiForgeryToken]
            public async Task<IActionResult> EditBai(EditBaiViewModel vmb)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
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
                var bai = await _context.Bai
                        .Include(b => b.Chuong).ThenInclude(c => c.BaiGiang)
                        .FirstOrDefaultAsync(b =>
                            b.IdBai == vmb.IdBai &&
                            b.Chuong.BaiGiang.IdTaiKhoan == userId);
                    if (bai == null) return NotFound();




                var listnameb = await _context.Bai
                    .Where(b => b.IdChuong == bai.IdChuong && b.IdBai != bai.IdBai)
                    .Select(b => b.TieuDeBai)
                    .ToListAsync();


                var  rname = vmb.TieuDeBai
                    .Trim()
                    .Replace(" ", "")
                    .ToLowerInvariant();


                bool check = listnameb
                    .Select(t => t.Trim().Replace(" ", "").ToLowerInvariant())
                    .Any(t => t == rname);

                if (check)
                {
                    ModelState.AddModelError(nameof(vmb.TieuDeBai), "Tên bài đã tồn tại.");
                    var errors = ModelState
                      .Where(x => x.Value.Errors.Any())
                      .ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                      );
                    return BadRequest(new { errors });
                }

                var UpdateNoiDung = await _imgHandle.ProcessImagesAsync(vmb.NoiDung, userId, vmb.IdChuong, vmb.IdBai, status: true);
                    bai.TieuDeBai = StringHelper.NormalizeString(vmb.TieuDeBai);
                    bai.NoiDungText = UpdateNoiDung;
                    bai.NgayCapNhat = DateTime.Now;
                    await _context.SaveChangesAsync();

                return Json(new { success = true });
            }

        [HttpGet]
        public async Task<IActionResult> AddBai(int idChuong)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var chuong = await _context.Chuong
                .Include(c => c.BaiGiang)
                .FirstOrDefaultAsync(c =>
                    c.IdChuong == idChuong &&
                    c.BaiGiang.IdTaiKhoan == userId);
            if (chuong == null)
                return NotFound();

            var vm = new CreateBaiViewModel
            {
                IdChuong = idChuong,
                IdBaiGiang = chuong.IdBaiGiang
            };
            return View(vm);
        }

        [RequestSizeLimit(6_000_000)]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBai(CreateBaiViewModel vm)
        {
    
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var chuong = await _context.Chuong
                .Include(c => c.BaiGiang)
                .FirstOrDefaultAsync(c =>
                    c.IdChuong == vm.IdChuong &&
                    c.BaiGiang.IdTaiKhoan == userId);
            if (chuong == null)
            {
                return BadRequest(new
                {
                    errors = new { IdChuong = new[] { "Chương không tồn tại hoặc bạn không có quyền." } }
                });
            }


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

            var checknameCh = vm.TieuDeBai
                .Trim()
                .Replace(" ", "")
                .ToLowerInvariant();
            bool duplicate = await _context.Bai
                .Where(b => b.IdChuong == vm.IdChuong)
                .AnyAsync(b =>
                    b.TieuDeBai.Trim()
                               .Replace(" ", "") == checknameCh);
            if (duplicate)
            {
                ModelState.AddModelError(nameof(vm.TieuDeBai), "Tên bài đã tồn tại.");
                var errors = ModelState
                    .Where(x => x.Value.Errors.Any())
                    .ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { errors });
            }
            var bai = new BaiModels
            {
                IdChuong = vm.IdChuong,
                TieuDeBai = StringHelper.NormalizeString(vm.TieuDeBai),
                NoiDungText = "",
                NgayTao = DateTime.Now,
                NgayCapNhat = DateTime.Now
            };
            _context.Bai.Add(bai);
            await _context.SaveChangesAsync();

            // Gọi service xử lý ảnh
            var addNewBai = await _imgHandle.ProcessImagesAsync(
                NoiDung: vm.NoiDung,
                userId: userId,
                idChuong: vm.IdChuong,   
                bai.IdBai,
                status: false        
            );

            var getbai = new BaiModels
            {
                IdChuong = vm.IdChuong,
                TieuDeBai = StringHelper.NormalizeString(vm.TieuDeBai),
                NoiDungText = addNewBai,
                NgayTao = DateTime.Now,
                NgayCapNhat = DateTime.Now
            };

            bai.NoiDungText = addNewBai;
            bai.NgayCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

    }
} 