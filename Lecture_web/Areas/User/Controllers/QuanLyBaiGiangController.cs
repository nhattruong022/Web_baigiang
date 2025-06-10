using Microsoft.AspNetCore.Mvc;

namespace Lecture_web.Areas.User.Controllers
{
    [Area("User")]
    public class QuanLyBaiGiangController : Controller
    {
        public IActionResult Index()
        {
            // TODO: Lấy danh sách lớp học từ database
            return View();
        }
    }
} 