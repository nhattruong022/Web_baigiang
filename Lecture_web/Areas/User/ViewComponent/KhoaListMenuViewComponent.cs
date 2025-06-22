using Lecture_web;
using Lecture_web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;



    public class KhoaListMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        public KhoaListMenuViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var listmenu = await _context.Khoa.OrderByDescending(k=>k.NgayTao)
                        .Include(b => b.BoMons.OrderByDescending(b=>b.NgayTao))
                        .ThenInclude(h => h.HocPhans.OrderByDescending(h=>h.NgayTao))
                        .ToListAsync();
        return View(listmenu);
        }
    }

