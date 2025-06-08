using Microsoft.AspNetCore.Mvc;

namespace Lecture_web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class QuanLyKhoaController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
} 