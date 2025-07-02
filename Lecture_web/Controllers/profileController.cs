using Microsoft.AspNetCore.Mvc;
using Lecture_web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace Lecture_web.Controllers
{
    [Authorize]
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
            Console.WriteLine("=== PROFILE DEBUG START ===");
            
            // Sử dụng ClaimTypes.NameIdentifier giống như top_bar để đảm bảo nhất quán
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"User ID claim: {userIdClaim}");
            Console.WriteLine($"User authenticated: {User.Identity.IsAuthenticated}");
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                Console.WriteLine("ERROR: No valid user ID claim found");
                return RedirectToAction("Login", "Account");
            }
            
            Console.WriteLine($"Looking for user with ID: {userId}");
            
            var user = _context.TaiKhoan.FirstOrDefault(u => u.IdTaiKhoan == userId);
            if (user == null)
            {
                Console.WriteLine("ERROR: User not found in database");
                return RedirectToAction("Login", "Account");
            }
            
            Console.WriteLine($"Found user: {user.HoTen}, Avatar: {user.AnhDaiDien}");
            
            // Xử lý avatar path đúng cách
            string avatarPath = ProcessAvatarPath(user.AnhDaiDien);
            
            Console.WriteLine($"Final avatar path: {avatarPath}");
            Console.WriteLine("=== PROFILE DEBUG END ===");
            
            ViewBag.Avatar = avatarPath;
            ViewBag.UserName = user.HoTen ?? user.TenDangNhap;
            return View(user);
        }

        private string ProcessAvatarPath(string avatarPath)
        {
            if (string.IsNullOrEmpty(avatarPath) || string.IsNullOrWhiteSpace(avatarPath))
            {
                Console.WriteLine("ProcessAvatarPath: Avatar path is null or empty, using default");
                return "/images/avatars/avatar.jpg";
            }

            string cleanPath = avatarPath.Trim();
            Console.WriteLine($"ProcessAvatarPath: Input path = '{cleanPath}'");

            // Nếu đã có đầy đủ path bắt đầu với /
            if (cleanPath.StartsWith("/"))
            {
                Console.WriteLine($"ProcessAvatarPath: Path starts with /, returning: '{cleanPath}'");
                return cleanPath;
            }

            // Nếu path bắt đầu với images/
            if (cleanPath.StartsWith("images/"))
            {
                string result = "/" + cleanPath;
                Console.WriteLine($"ProcessAvatarPath: Path starts with images/, returning: '{result}'");
                return result;
            }

            // Nếu chỉ có tên file
            string finalPath = "/images/avatars/" + cleanPath;
            Console.WriteLine($"ProcessAvatarPath: File name only, returning: '{finalPath}'");
            return finalPath;
        }

        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }


            [HttpPost]
        public IActionResult EditProfilAjax(string HoTen, string Email, string SDT)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return RedirectToAction("Login", "Account");
                var user = _context.TaiKhoan.FirstOrDefault(u => u.IdTaiKhoan == userId);
                if (user == null)
                    return RedirectToAction("Login", "Account");
                user.HoTen = HoTen;
                user.Email = Email;
                user.SoDienThoai = SDT;
                user.NgayCapNhat = DateTime.Now;
                _context.TaiKhoan.Update(user);
                _context.SaveChanges();
                return RedirectToAction("profile");
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            try
            {
                Console.WriteLine("=== UploadAvatar DEBUG START ===");
                
                // Sử dụng ClaimTypes.NameIdentifier để đảm bảo nhất quán
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Console.WriteLine($"User ID claim: {userIdClaim}");
                Console.WriteLine($"User authenticated: {User.Identity.IsAuthenticated}");
                Console.WriteLine($"Avatar null: {avatar == null}");
                Console.WriteLine($"Avatar length: {avatar?.Length ?? 0}");
                
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    Console.WriteLine("ERROR: No valid user ID claim found");
                    return Json(new { success = false, message = "Người dùng chưa đăng nhập." });
                }
                
                if (avatar == null || avatar.Length == 0)
                {
                    Console.WriteLine("ERROR: No file uploaded");
                    return Json(new { success = false, message = "Vui lòng chọn file ảnh." });
                }

                // Kiểm tra loại file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    Console.WriteLine($"ERROR: Invalid file extension: {fileExtension}");
                    return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif)." });
                }

                // Kiểm tra kích thước file (max 5MB)
                if (avatar.Length > 5 * 1024 * 1024)
                {
                    Console.WriteLine($"ERROR: File too large: {avatar.Length} bytes");
                    return Json(new { success = false, message = "File ảnh phải nhỏ hơn 5MB." });
                }
                
                var user = _context.TaiKhoan.FirstOrDefault(u => u.IdTaiKhoan == userId);
                if (user == null)
                {
                    Console.WriteLine("ERROR: User not found");
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                Console.WriteLine($"User found: ID={user.IdTaiKhoan}, Username={user.TenDangNhap}, Name={user.HoTen}");
                Console.WriteLine($"Current avatar: {user.AnhDaiDien}");

                // Tạo thư mục avatars nếu chưa có
                var avatarsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
                if (!Directory.Exists(avatarsDir))
                {
                    Directory.CreateDirectory(avatarsDir);
                    Console.WriteLine($"Created avatars directory: {avatarsDir}");
                }

                // Xóa avatar cũ nếu có
                if (!string.IsNullOrEmpty(user.AnhDaiDien) && user.AnhDaiDien.StartsWith("/images/avatars/"))
                {
                    var oldAvatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AnhDaiDien.TrimStart('/'));
                    if (System.IO.File.Exists(oldAvatarPath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldAvatarPath);
                            Console.WriteLine($"Deleted old avatar: {oldAvatarPath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"WARNING: Could not delete old avatar: {ex.Message}");
                        }
                    }
                }
                
                var fileName = $"avatar_{user.IdTaiKhoan}_{DateTime.Now.Ticks}{fileExtension}";
                var savePath = Path.Combine(avatarsDir, fileName);
                
                Console.WriteLine($"Saving to: {savePath}");
                
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }
                
                Console.WriteLine("File saved successfully");
                
                user.AnhDaiDien = $"/images/avatars/{fileName}";
                user.NgayCapNhat = DateTime.Now;
                _context.TaiKhoan.Update(user);
                var saveResult = await _context.SaveChangesAsync();
                
                Console.WriteLine($"Database updated. Rows affected: {saveResult}");
                Console.WriteLine($"New avatar URL: {user.AnhDaiDien}");
                Console.WriteLine("=== UploadAvatar DEBUG END ===");
                
                return Json(new { 
                    success = true, 
                    url = user.AnhDaiDien,
                    message = "Cập nhật ảnh đại diện thành công!",
                    debug = new {
                        fileName = fileName,
                        fileSize = avatar.Length,
                        userId = user.IdTaiKhoan
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UploadAvatar: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Json(new { 
                    success = false, 
                    message = $"Lỗi server: {ex.Message}" 
                });
            }
        }

        [HttpPost]
        public IActionResult ChangePassword(string oldPassword, string newPassword)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Json(new { success = false, message = "Tài khoản không tồn tại" });
            var user = _context.TaiKhoan.FirstOrDefault(u => u.IdTaiKhoan == userId);
            if (user == null)
                return Json(new { success = false, message = "Tài khoản không tồn tại" });
            if (user.MatKhau != oldPassword)
                return Json(new { success = false, message = "Mật khẩu cũ không đúng" });
            user.MatKhau = newPassword;
            user.NgayCapNhat = DateTime.Now;
            _context.TaiKhoan.Update(user);
            _context.SaveChanges();
            return Json(new { success = true, message = "Đổi mật khẩu thành công" });
        }
        
       
    }
} 