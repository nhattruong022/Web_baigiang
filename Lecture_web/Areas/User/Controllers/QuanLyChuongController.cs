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
        public async Task<IActionResult> Index(string search, int page = 1)
        {

            const int pageSize = 5;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));


            var q = _context.BaiGiang
                .Include(c=>c.Chuongs.OrderByDescending(o=>o.NgayCapNhat))
                .Where(b => b.IdTaiKhoan == userId)
                .Select(p => new ListChuongViewModel
                {
                    IdChuong = p.Chuongs.Select(c=>c.IdChuong).FirstOrDefault(),
                    Ten = p.Chuongs.Select(c=>c.TenChuong).FirstOrDefault(),
                    BaiGiang = p.TieuDe,
                    NgayTao = p.Chuongs.Select(c=>c.NgayTao).FirstOrDefault(),
                    NgayCapNhat = p.Chuongs.Select(c=>c.NgayCapNhat).FirstOrDefault(),
                });


            if (!string.IsNullOrWhiteSpace(search))
            {
                var key = search.Replace(" ", "").ToLower();
                q = q.Where(x =>
                  x.Ten.Replace(" ", "")
                          .ToLower()
                          .Contains(key)
                );
            }

            var totalItems = await q.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();


            var bg = new SearchChuongViewModel<ListChuongViewModel>
            {
                Items = items,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchTerm = search
            };

            return View(bg);
        }
    }
} 