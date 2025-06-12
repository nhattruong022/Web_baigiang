using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Lecture_web.Models.ViewModels;
using Microsoft.AspNetCore.Components.Forms;

namespace Lecture_web.Areas.User.Controllers
{
    [Area("User")]
    public class QuanLyBaiGiangController : Controller
    {
        private readonly ApplicationDbContext _context;
        public QuanLyBaiGiangController(ApplicationDbContext context)
        {
            _context = context;
        }
        //public IActionResult Index()
        //{
        //    // TODO: Lấy danh sách lớp học từ database
        //    return View();
        //}
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idClaim, out var userId))
                return Unauthorized();


            var list = await _context.BaiGiang
               .Where(b => b.IdTaiKhoan == userId)
               .Include(b => b.LopHocPhans)
                   .ThenInclude(lp => lp.HocPhan).Select(p => new BaiGiangViewModel()
                   {
                       IdBaiGiang = p.IdBaiGiang,
                       TieuDe = p.TieuDe,
                       MoTa = p.MoTa,
                       LopHocPhan = string.Join(", ",
                       p.LopHocPhans.Select(hp => hp.HocPhan.TenHocPhan + "-" + hp.TenLop)
                       ),
                       NgayTao = p.NgayTao,
                       NgayCapNhat = p.NgayCapNhat,

                   }).ToListAsync();
            ViewBag.list = list;



            return View();
        }

        public async Task<IActionResult>Edit(int idbg)
        {
           
                var edit = await _context.BaiGiang
                    .Where(bg => bg.IdBaiGiang == idbg)
                    .Include(b => b.LopHocPhans).ThenInclude(lp => lp.HocPhan)
                    .Select(p => new BaiGiangViewModel
                    {
                        IdBaiGiang = p.IdBaiGiang,
                        TieuDe = p.TieuDe,
                        MoTa = p.MoTa,
                        LopHocPhan = string.Join(", ",
                                            p.LopHocPhans
                                            .Select(lp => lp.HocPhan.TenHocPhan + "-" + lp.TenLop)),
                        NgayTao = p.NgayTao,
                        NgayCapNhat = p.NgayCapNhat
                    })
                    .FirstOrDefaultAsync();
            
            return PartialView("Edit",edit);
        }

    }
}