using Microsoft.AspNetCore.Mvc;

namespace Lecture_web.Areas.User.Controllers
{
    [Area("User")]
    public class QuanLyBaiController : Controller
    {
        public IActionResult Index()
        {
            // TODO: Lấy danh sách lớp học từ database
            return View();
        }

        public IActionResult AddBai()
        {
          return View();
        }

        public IActionResult EditBai()
        {
          return View();
        }

    }
} 