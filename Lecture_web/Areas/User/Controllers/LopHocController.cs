using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Lecture_web.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Giangvien,Sinhvien")]
    public class LopHocController : Controller
    {
        public IActionResult Index()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            ViewBag.UserRole = userRole;
            // TODO: Lấy danh sách lớp học từ database
            return View();
        }
    }
    
} 