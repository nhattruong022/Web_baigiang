using Lecture_web;
using Lecture_web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;



public class BaiGiangListMenuViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    public BaiGiangListMenuViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var bg = await _context.BaiGiang
            .Where(b => b.IdTaiKhoan == userId)
            .OrderBy(b => b.TieuDe)
            .OrderByDescending(b => b.NgayTao)
            .ToListAsync();   

        return View(bg);
    }
}

