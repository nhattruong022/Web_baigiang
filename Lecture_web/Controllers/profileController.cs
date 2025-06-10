using Microsoft.AspNetCore.Mvc;
using Lecture_web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Lecture_web.Controllers
{
    public class profileController : Controller
    {
        private readonly ApplicationDbContext _context;
        public profileController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult profile()
        {
            return View();
        }

        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

       
      
    }
} 