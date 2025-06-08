using Microsoft.AspNetCore.Mvc;

namespace Lecture_web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class QuanLyBoMonController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
} 