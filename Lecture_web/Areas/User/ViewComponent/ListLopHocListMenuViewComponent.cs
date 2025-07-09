using Lecture_web;
using Lecture_web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Lecture_web.Models.ViewModels;


public class ListLopHocListMenuViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    public ListLopHocListMenuViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<IViewComponentResult> InvokeAsync()
    {
        List<ListLopViewModel> lst = new List<ListLopViewModel>();
        var userId = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var Role = HttpContext.User.FindFirstValue(ClaimTypes.Role);
        if (Role== "Giangvien")
        {
            lst = await _context.LopHocPhan 
            .Where(l => l.IdTaiKhoan == userId)
            .OrderByDescending(l => l.NgayTao).Select(l=>new ListLopViewModel
            {
                idLopHocPhan = l.IdLopHocPhan,
                Name = l.HocPhan.TenHocPhan + "-" +l.TenLop
            }).ToListAsync();
        }
        if (Role == "Sinhvien")
        {
            lst = await _context.LopHocPhan_SinhVien
            .Where(l => l.IdTaiKhoan == userId)
            .OrderByDescending(l => l.idLop_SinhVien)
            .Select(l => new ListLopViewModel
            {
                idLopHocPhan = l.LopHocPhan.IdLopHocPhan,
                Name = l.LopHocPhan.HocPhan.TenHocPhan + "-" + l.LopHocPhan.TenLop
            }).ToListAsync();
        }



        return View(lst);
    }
}

