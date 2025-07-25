using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Lecture_web.Models.ViewModels;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Lecture_web.Service;
using Lecture_web.Models;
using Microsoft.AspNetCore.SignalR;
using Lecture_web.Hubs;
using Microsoft.AspNetCore.Http;

namespace Lecture_web.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Giangvien,Sinhvien")]
    public class ChiTietHocPhanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ChiTietHocPhanController(ApplicationDbContext context, IEmailService emailService, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _emailService = emailService;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index(int? idLopHocPhan, int page = 1)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            const int pageSize = 5; // Số sinh viên mỗi trang (giảm để test phân trang)
            
            // Lấy thông tin user hiện tại
            var currentUser = await _context.TaiKhoan
                .FirstOrDefaultAsync(tk => tk.IdTaiKhoan == currentUserId);
            
            ViewBag.UserRole = userRole;
            ViewBag.CurrentUser = currentUser;
            ViewBag.CurrentUserId = currentUserId;

            

            try
            {
                // Lấy danh sách lớp học phần mà user có quyền truy cập
                IQueryable<LopHocPhanModels> accessibleClasses;
                
                if (userRole == "Giangvien")
                {
                    // Giảng viên chỉ xem được lớp của mình
                    accessibleClasses = _context.LopHocPhan.Where(lhp => lhp.IdTaiKhoan == currentUserId);
                }
                else if (userRole == "Sinhvien")
                {
                    // Sinh viên chỉ xem được lớp mình tham gia
                    accessibleClasses = _context.LopHocPhan
                        .Where(lhp => lhp.LopHocPhan_SinhViens.Any(sv => sv.IdTaiKhoan == currentUserId));
                }
                else
                {
                    // Vai trò khác không có quyền truy cập
        
                    return Unauthorized();
                }

                var allLopHocPhan = await accessibleClasses
                    .Include(lhp => lhp.HocPhan)
                    .Select(lhp => new 
                    { 
                        lhp.IdLopHocPhan,
                        lhp.TenLop,
                        TenHocPhan = lhp.HocPhan.TenHocPhan,
                        DisplayName = lhp.HocPhan.TenHocPhan + " - " + lhp.TenLop
                    })
                    .OrderBy(lhp => lhp.TenHocPhan)
                    .ThenBy(lhp => lhp.TenLop)
                    .ToListAsync();

                ViewBag.AllLopHocPhan = allLopHocPhan;



                // Kiểm tra nếu có idLopHocPhan được chỉ định
                if (idLopHocPhan.HasValue)
                {
                    // Kiểm tra user có quyền truy cập lớp học này không
                    var hasAccess = allLopHocPhan.Any(lhp => lhp.IdLopHocPhan == idLopHocPhan.Value);
                    if (!hasAccess)
                    {
                        
                        return Forbid(); // HTTP 403 - Forbidden
                    }
                }

                // Nếu không có idLopHocPhan, lấy lớp học phần đầu tiên mà user có quyền truy cập
                int targetLopHocPhanId = idLopHocPhan ?? allLopHocPhan.FirstOrDefault()?.IdLopHocPhan ?? 0;
                
                if (targetLopHocPhanId == 0)
                {
        
                    ViewBag.ErrorMessage = userRole == "Giangvien" ? 
                        "Bạn chưa tạo lớp học nào. Vui lòng tạo lớp học trước." :
                        "Bạn chưa tham gia lớp học nào. Vui lòng liên hệ giảng viên để được mời vào lớp.";
                    ViewBag.AllLopHocPhan = allLopHocPhan;
                    // Sửa: trả về view mặc định với dữ liệu rỗng
                    ViewBag.Chuongs = new List<object>();
                    return View();
                }
                
    

                // Lấy thông tin lớp học phần từ accessible classes
                var lopHocPhan = await accessibleClasses
                    .Include(lhp => lhp.HocPhan)
                    .FirstOrDefaultAsync(lhp => lhp.IdLopHocPhan == targetLopHocPhanId);

                if (lopHocPhan == null)
                {
                    // Nếu không tìm thấy, tạo dữ liệu mặc định
                    ViewBag.IdLopHocPhan = targetLopHocPhanId;
                    ViewBag.TenLop = "LTCB23-01";
                    ViewBag.IdBaiGiang = 1;
                    ViewBag.StudentsInClass = new List<object>();
                    ViewBag.Chuongs = new List<object>();
                    ViewBag.AllLopHocPhan = allLopHocPhan;
                    
                    // Thông tin phân trang mặc định
                    ViewBag.CurrentPage = 1;
                    ViewBag.TotalPages = 1;
                    ViewBag.TotalStudents = 0;
                    ViewBag.PageSize = pageSize;
                    ViewBag.ChangePageFunc = "changeStudentPage";
                    return View();
                }

                // Lấy dầy đủ dữ liệu chương và bài từ BaiGiang
                var chuongs = await _context.Chuong
                    .Where(c => c.IdBaiGiang == lopHocPhan.IdBaiGiang)
                    .Select(c => new
                    {
                        c.IdChuong,
                        c.TenChuong,
                        c.NgayTao,
                        c.IdBaiGiang,
                        Bais = _context.Bai
                            .Where(b => b.IdChuong == c.IdChuong)
                            .Select(b => new
                            {
                                b.IdBai,
                                b.TieuDeBai,
                                b.NoiDungText,
                                b.NgayTao,
                                b.IdChuong
                            })
                            .OrderBy(b => b.NgayTao)
                            .ToList()
                    })
                    .OrderBy(c => c.NgayTao)
                    .ToListAsync();

                
                


                // Nếu không có dữ liệu thực, trả về danh sách rỗng
                if (!chuongs.Any())
                {
                    Console.WriteLine($"No chuongs found for IdBaiGiang={lopHocPhan.IdBaiGiang}");
                }



                // Lấy tổng số sinh viên trong lớp để tính phân trang
                var totalStudents = await _context.LopHocPhan_SinhVien
                    .Where(lhp_sv => lhp_sv.IdLopHocPhan == targetLopHocPhanId)
                    .Join(_context.TaiKhoan,
                          lhp_sv => lhp_sv.IdTaiKhoan,
                          tk => tk.IdTaiKhoan,
                          (lhp_sv, tk) => tk)
                    .Where(tk => tk.VaiTro == "Sinhvien")
                    .CountAsync();

                var totalPages = (int)Math.Ceiling((double)totalStudents / pageSize);
                var currentPage = Math.Max(1, Math.Min(page, totalPages));

                // Lấy danh sách sinh viên trong lớp với phân trang (chỉ lấy những user có vai trò Sinhvien)
                var studentsInClass = await _context.LopHocPhan_SinhVien
                    .Where(lhp_sv => lhp_sv.IdLopHocPhan == targetLopHocPhanId)
                    .Join(_context.TaiKhoan,
                          lhp_sv => lhp_sv.IdTaiKhoan,
                          tk => tk.IdTaiKhoan,
                          (lhp_sv, tk) => new
                          {
                              tk.IdTaiKhoan,
                              tk.TenDangNhap,
                              tk.HoTen,
                              tk.Email,
                              tk.SoDienThoai,
                              tk.AnhDaiDien,
                              tk.TrangThai,
                              tk.VaiTro
                          })
                    .Where(tk => tk.VaiTro == "Sinhvien") // Chỉ lấy sinh viên
                    .OrderBy(tk => tk.HoTen)
                    .Skip((currentPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.IdLopHocPhan = targetLopHocPhanId;
                ViewBag.TenLop = lopHocPhan.TenLop;
                ViewBag.IdBaiGiang = lopHocPhan.IdBaiGiang;
                ViewBag.StudentsInClass = studentsInClass;
                ViewBag.Chuongs = chuongs; // Truyền dữ liệu chương và bài sang view
                
                // Thông tin phân trang cho sinh viên
                ViewBag.CurrentPage = currentPage;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalStudents = totalStudents;
                ViewBag.PageSize = pageSize;
                ViewBag.ChangePageFunc = "changeStudentPage";
                

                

                
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in Index: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                
                // Trả về view mặc định với thông báo lỗi
                ViewBag.ErrorMessage = $"Lỗi hệ thống: {ex.Message}";
                ViewBag.UserRole = userRole;
                ViewBag.AllLopHocPhan = new List<object>();
                ViewBag.Chuongs = new List<object>();
                return View();
            }
        }

        // API để tìm kiếm sinh viên theo nhiều tiêu chí
        [HttpGet]
        public async Task<IActionResult> SearchStudents(string searchTerm, string searchType = "all")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return Json(new { success = false, message = "Từ khóa tìm kiếm không được để trống" });
                }

                var query = _context.TaiKhoan
                    .Where(tk => tk.VaiTro == "Sinhvien" && tk.TrangThai == "HoatDong");

                // Tìm kiếm theo loại
                switch (searchType.ToLower())
                {
                    case "email":
                        query = query.Where(tk => tk.Email.Contains(searchTerm));
                        break;
                    case "name":
                        query = query.Where(tk => tk.HoTen.Contains(searchTerm));
                        break;
                    case "username":
                        query = query.Where(tk => tk.TenDangNhap.Contains(searchTerm));
                        break;
                    case "phone":
                        query = query.Where(tk => tk.SoDienThoai.Contains(searchTerm));
                        break;
                    default: // "all"
                        query = query.Where(tk => tk.Email.Contains(searchTerm) || 
                                                 tk.HoTen.Contains(searchTerm) ||
                                                 tk.TenDangNhap.Contains(searchTerm) ||
                                                 (tk.SoDienThoai != null && tk.SoDienThoai.Contains(searchTerm)));
                        break;
                }

                var students = await query
                    .Select(tk => new 
                    {
                        tk.IdTaiKhoan,
                        tk.TenDangNhap,
                        tk.Email,
                        tk.HoTen,
                        tk.SoDienThoai,
                        tk.AnhDaiDien,
                        tk.TrangThai
                    })
                    .OrderBy(tk => tk.HoTen)
                    .Take(20)
                    .ToListAsync();

                return Json(new { success = true, data = students });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi tìm kiếm: {ex.Message}" });
            }
        }

        // API để lấy thông tin sinh viên theo ID
        [HttpGet]
        public async Task<IActionResult> GetStudentInfo(int idTaiKhoan)
        {
            try
            {
                var student = await _context.TaiKhoan
                    .Where(tk => tk.IdTaiKhoan == idTaiKhoan && tk.VaiTro == "Sinhvien")
                    .Select(tk => new
                    {
                        tk.IdTaiKhoan,
                        tk.TenDangNhap,
                        tk.Email,
                        tk.HoTen,
                        tk.SoDienThoai,
                        tk.AnhDaiDien,
                        tk.TrangThai
                    })
                    .FirstOrDefaultAsync();

                if (student == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sinh viên" });
                }

                return Json(new { success = true, data = student });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi lấy thông tin sinh viên: {ex.Message}" });
            }
        }

        // API để cập nhật thông tin sinh viên
        [HttpPost]
        [Authorize(Roles = "Giangvien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStudent(int idTaiKhoan, string hoTen, string email, string soDienThoai)
        {
            try
            {
                var student = await _context.TaiKhoan
                    .FirstOrDefaultAsync(tk => tk.IdTaiKhoan == idTaiKhoan && tk.VaiTro == "Sinhvien");

                if (student == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sinh viên" });
                }

                // Kiểm tra email đã tồn tại chưa (trừ sinh viên hiện tại)
                var existingUser = await _context.TaiKhoan
                    .FirstOrDefaultAsync(tk => tk.Email == email && tk.IdTaiKhoan != idTaiKhoan);
                
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "Email đã được sử dụng bởi tài khoản khác" });
                }

                // Cập nhật thông tin
                student.HoTen = hoTen?.Trim();
                student.Email = email?.Trim();
                student.SoDienThoai = soDienThoai?.Trim();
                student.NgayCapNhat = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Cập nhật thông tin sinh viên thành công",
                    data = new {
                        student.IdTaiKhoan,
                        student.TenDangNhap,
                        student.HoTen,
                        student.Email,
                        student.SoDienThoai,
                        student.AnhDaiDien,
                        student.TrangThai
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi cập nhật sinh viên: {ex.Message}" });
            }
        }

        // API để xóa sinh viên khỏi lớp
        [HttpPost]
        [Authorize(Roles = "Giangvien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudentFromClass(int idLopHocPhan, int idTaiKhoan)
        {
            try
            {
                // Kiểm tra xem sinh viên có trong lớp không
                var membership = await _context.LopHocPhan_SinhVien
                    .FirstOrDefaultAsync(lhp_sv => lhp_sv.IdLopHocPhan == idLopHocPhan && 
                                                  lhp_sv.IdTaiKhoan == idTaiKhoan);

                if (membership == null)
                {
                    return Json(new { success = false, message = "Sinh viên không có trong lớp này" });
                }

                // Xóa khỏi lớp
                _context.LopHocPhan_SinhVien.Remove(membership);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Đã xóa sinh viên khỏi lớp"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi xóa sinh viên khỏi lớp: {ex.Message}" });
            }
        }

        // API để gửi lời mời tham gia lớp học (nhiều sinh viên)
        [HttpPost]
        [Authorize(Roles = "Giangvien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteStudents(int idLopHocPhan, int[] idTaiKhoans)
        {
            try
            {
                if (idTaiKhoans == null || idTaiKhoans.Length == 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn ít nhất một sinh viên để mời" });
                }

                // Lấy thông tin lớp học phần
                var lopHocPhan = await _context.LopHocPhan
                    .FirstOrDefaultAsync(lhp => lhp.IdLopHocPhan == idLopHocPhan);

                if (lopHocPhan == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lớp học phần" });
                }

                // Lấy thông tin tất cả sinh viên được chọn
                var students = await _context.TaiKhoan
                    .Where(tk => idTaiKhoans.Contains(tk.IdTaiKhoan) && 
                                tk.VaiTro == "Sinhvien" && 
                                tk.TrangThai == "HoatDong")
                    .ToListAsync();

                // Kiểm tra các sinh viên đã tham gia lớp chưa
                var existingMembers = await _context.LopHocPhan_SinhVien
                    .Where(lhp_sv => lhp_sv.IdLopHocPhan == idLopHocPhan && 
                                    idTaiKhoans.Contains(lhp_sv.IdTaiKhoan))
                    .Select(lhp_sv => lhp_sv.IdTaiKhoan)
                    .ToListAsync();

                // Lọc ra những sinh viên chưa tham gia lớp
                var studentsToInvite = students
                    .Where(s => !existingMembers.Contains(s.IdTaiKhoan))
                    .ToList();

                if (studentsToInvite.Count == 0)
                {
                    return Json(new { success = false, message = "Tất cả sinh viên đã tham gia lớp học này rồi" });
                }

                var successfulInvites = new List<string>();
                var failedInvites = new List<string>();

                // Gửi lời mời cho từng sinh viên
                foreach (var student in studentsToInvite)
                {
                    try
                    {
                        // Tạo token mời riêng cho mỗi sinh viên
                        var inviteToken = Guid.NewGuid().ToString();
                        var inviteExpiry = DateTime.Now.AddDays(7);
                        
                        // Lưu thông tin lời mời vào database
                        var invitation = new ThongBaoModels
                        {
                            IdTaiKhoan = student.IdTaiKhoan,
                            IdLopHocPhan = idLopHocPhan,
                            NoiDung = $"INVITE|{inviteToken}|{idLopHocPhan}|Lời mời tham gia lớp: {lopHocPhan.TenLop}",
                            NgayTao = DateTime.Now,
                            NgayCapNhat = DateTime.Now
                        };

                        _context.ThongBao.Add(invitation);

                        // Gửi email mời
                        var acceptUrl = $"{Request.Scheme}://{Request.Host}/User/ChiTietHocPhan/AcceptInvitation?token={inviteToken}";
                        var emailBody = $@"
                            <h2>Lời mời tham gia lớp học</h2>
                            <p>Chào {student.HoTen},</p>
                            <p>Bạn được mời tham gia lớp học: <strong>{lopHocPhan.TenLop}</strong></p>
                            <p>Mô tả: {lopHocPhan.MoTa ?? "Không có mô tả"}</p>
                            <p>Vui lòng click vào link bên dưới để tham gia:</p>
                            <a href='{acceptUrl}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Tham gia lớp học</a>
                            <p><small>Link này có hiệu lực đến {inviteExpiry:dd/MM/yyyy HH:mm}</small></p>
                        ";

                        try 
                        {
                            await _emailService.SendEmailAsync(student.Email, $"Lời mời tham gia lớp {lopHocPhan.TenLop}", emailBody);
                            successfulInvites.Add($"{student.HoTen} ({student.Email})");
                        }
                        catch (Exception emailEx)
                        {
                            successfulInvites.Add($"{student.HoTen} ({student.Email}) - Email failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        failedInvites.Add($"{student.HoTen} ({student.Email}): {ex.Message}");
                    }
                }

                // Lưu tất cả invitations vào database
                await _context.SaveChangesAsync();

                // Tạo message kết quả
                var resultMessage = $"Đã gửi lời mời thành công cho {successfulInvites.Count} sinh viên";
                
                if (existingMembers.Count > 0)
                {
                    resultMessage += $" ({existingMembers.Count} sinh viên đã tham gia lớp)";
                }
                
                if (failedInvites.Count > 0)
                {
                    resultMessage += $"\nLỗi: {string.Join(", ", failedInvites)}";
                }

                return Json(new { 
                    success = true, 
                    message = resultMessage,
                    details = new {
                        successful = successfulInvites.Count,
                        failed = failedInvites.Count,
                        alreadyMembers = existingMembers.Count,
                        successfulList = successfulInvites,
                        failedList = failedInvites
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi gửi lời mời: {ex.Message}" });
            }
        }

        // Action để xử lý accept invitation
        [HttpGet]
        [AllowAnonymous] // Cho phép access mà không cần đăng nhập
        public async Task<IActionResult> AcceptInvitation(string token)
        {
            try
            {
                Console.WriteLine($"DEBUG AcceptInvitation: Received token={token}");
                Console.WriteLine($"DEBUG AcceptInvitation: User authenticated={User.Identity.IsAuthenticated}");
                
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine($"DEBUG AcceptInvitation: Token is null or empty");
                    TempData["Error"] = "Token không hợp lệ";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                // Tìm thông báo mời
                var invitation = await _context.ThongBao
                    .FirstOrDefaultAsync(tb => tb.NoiDung.Contains(token) && tb.NoiDung.StartsWith("INVITE|"));

                Console.WriteLine($"DEBUG AcceptInvitation: Invitation found={invitation != null}");
                
                if (invitation == null)
                {
                    Console.WriteLine($"DEBUG AcceptInvitation: Invitation not found or expired");
                    TempData["Error"] = "Lời mời không tồn tại hoặc đã hết hạn";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                // Parse token để lấy IdLopHocPhan
                var tokenParts = invitation.NoiDung.Split("|");
                if (tokenParts.Length < 3 || !int.TryParse(tokenParts[2], out int idLopHocPhan))
                {
                    Console.WriteLine($"DEBUG AcceptInvitation: Invalid token format");
                    TempData["Error"] = "Token không hợp lệ";
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                Console.WriteLine($"DEBUG AcceptInvitation: Parsed idLopHocPhan={idLopHocPhan}");

                // Kiểm tra user có đăng nhập không
                if (!User.Identity.IsAuthenticated)
                {
                    Console.WriteLine($"DEBUG AcceptInvitation: User not authenticated, redirecting to login");
                    // Tạo returnUrl để sau khi đăng nhập sẽ redirect lại
                    var returnUrl = Url.Action("AcceptInvitation", "ChiTietHocPhan", new { area = "User", token = token });
                    Console.WriteLine($"DEBUG AcceptInvitation: ReturnUrl={returnUrl}");
                    TempData["Info"] = "Vui lòng đăng nhập để tham gia lớp học";
                    return RedirectToAction("Login", "Account", new { area = "", returnUrl = returnUrl });
                }

                // Kiểm tra user hiện tại có phải là người được mời không
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                Console.WriteLine($"DEBUG AcceptInvitation: currentUserId={currentUserId}, invitedUserId={invitation.IdTaiKhoan}");
                
                if (invitation.IdTaiKhoan != currentUserId)
                {
                    Console.WriteLine($"DEBUG AcceptInvitation: User mismatch - current={currentUserId}, invited={invitation.IdTaiKhoan}");
                    TempData["Error"] = "Bạn không có quyền sử dụng link này";
                    return RedirectToAction("Index", "LopHoc", new { area = "User" });
                }

                // Kiểm tra đã tham gia chưa
                var existingMember = await _context.LopHocPhan_SinhVien
                    .FirstOrDefaultAsync(lhp_sv => lhp_sv.IdLopHocPhan == idLopHocPhan && 
                                                  lhp_sv.IdTaiKhoan == currentUserId);

                Console.WriteLine($"DEBUG AcceptInvitation: Already member={existingMember != null}");

                if (existingMember != null)
                {
                    Console.WriteLine($"DEBUG AcceptInvitation: User already member, redirecting to class");
                    TempData["Warning"] = "Bạn đã tham gia lớp học này rồi";
                    return RedirectToAction("Index", "ChiTietHocPhan", new { area = "User", idLopHocPhan = idLopHocPhan });
                }

                // Thêm sinh viên vào lớp
                var newMember = new LopHocPhan_SinhVienModels
                {
                    IdLopHocPhan = idLopHocPhan,
                    IdTaiKhoan = currentUserId
                };

                Console.WriteLine($"DEBUG AcceptInvitation: Adding new member to class");
                _context.LopHocPhan_SinhVien.Add(newMember);

                // Đánh dấu tất cả lời mời INVITE cùng lớp của sinh viên này thành USED
                var allInvites = await _context.ThongBao
                    .Where(tb => tb.IdTaiKhoan == currentUserId && tb.IdLopHocPhan == idLopHocPhan && tb.NoiDung.StartsWith("INVITE|"))
                    .ToListAsync();
                foreach (var invite in allInvites)
                {
                    invite.NoiDung = invite.NoiDung.Replace("INVITE|", "USED|");
                    invite.NgayCapNhat = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"DEBUG AcceptInvitation: Successfully saved to database");

                TempData["Success"] = "Bạn đã tham gia lớp học thành công!";
                Console.WriteLine($"DEBUG AcceptInvitation: Redirecting to class detail page");
                return RedirectToAction("Index", "ChiTietHocPhan", new { area = "User", idLopHocPhan = idLopHocPhan });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG AcceptInvitation: Exception={ex.Message}");
                Console.WriteLine($"DEBUG AcceptInvitation: StackTrace={ex.StackTrace}");
                TempData["Error"] = $"Lỗi xử lý lời mời: {ex.Message}";
                return RedirectToAction("Login", "Account", new { area = "" });
            }
        }

        // API để reload danh sách sinh viên
        [HttpGet]
        public async Task<IActionResult> GetStudentsList(int idLopHocPhan, int page = 1)
        {
            try
            {
                const int pageSize = 1; // Số sinh viên mỗi trang (giảm để test phân trang)
                
                // Lấy tổng số sinh viên
                var totalStudents = await _context.LopHocPhan_SinhVien
                    .Where(lhp_sv => lhp_sv.IdLopHocPhan == idLopHocPhan)
                    .Join(_context.TaiKhoan,
                          lhp_sv => lhp_sv.IdTaiKhoan,
                          tk => tk.IdTaiKhoan,
                          (lhp_sv, tk) => tk)
                    .Where(tk => tk.VaiTro == "Sinhvien")
                    .CountAsync();

                var totalPages = (int)Math.Ceiling((double)totalStudents / pageSize);
                var currentPage = Math.Max(1, Math.Min(page, totalPages));

                var studentsInClass = await _context.LopHocPhan_SinhVien
                    .Where(lhp_sv => lhp_sv.IdLopHocPhan == idLopHocPhan)
                    .Join(_context.TaiKhoan,
                          lhp_sv => lhp_sv.IdTaiKhoan,
                          tk => tk.IdTaiKhoan,
                          (lhp_sv, tk) => new
                          {
                              tk.IdTaiKhoan,
                              tk.TenDangNhap,
                              tk.HoTen,
                              tk.Email,
                              tk.SoDienThoai,
                              tk.AnhDaiDien,
                              tk.TrangThai,
                              tk.VaiTro
                          })
                    .Where(x => x.VaiTro == "Sinhvien") // Chỉ lấy sinh viên
                    .OrderBy(x => x.HoTen)
                    .Skip((currentPage - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Json(new { 
                    success = true, 
                    data = studentsInClass,
                    pagination = new {
                        currentPage = currentPage,
                        totalPages = totalPages,
                        totalStudents = totalStudents,
                        pageSize = pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG GetStudentsList: Exception={ex.Message}");
                return Json(new { success = false, message = $"Lỗi tải danh sách sinh viên: {ex.Message}" });
            }
        }





        // API: Thêm bình luận hoặc reply
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment([FromBody] BinhLuanModels model)
        {
             try
            {
                Console.WriteLine($"DEBUG: AddComment called - NoiDung: {model.NoiDung}, IdBai: {model.IdBai}, IdLopHocPhan: {model.IdLopHocPhan}");
                
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                model.IdTaiKhoan = userId;
                model.NgayTao = DateTime.Now;

                Console.WriteLine($"DEBUG: User info - ID: {userId}, Role: {userRole}, Name: {User.Identity.Name}");

                // Validate dữ liệu bắt buộc
                if (string.IsNullOrWhiteSpace(model.NoiDung) || model.IdLopHocPhan == 0)
                {
                    Console.WriteLine($"DEBUG: Validation failed - NoiDung empty: {string.IsNullOrWhiteSpace(model.NoiDung)}, IdLopHocPhan: {model.IdLopHocPhan}");
                    return Json(new { success = false, message = "Thiếu dữ liệu bình luận hoặc lớp học phần." });
                }

                // Kiểm tra quyền reply: chỉ giảng viên mới được phản hồi bình luận
                if (model.IdBinhLuanCha.HasValue && model.IdBinhLuanCha > 0 && userRole != "Giangvien")
                {
                    Console.WriteLine($"DEBUG: Student {userId} attempted to reply comment {model.IdBinhLuanCha}");
                    return Json(new { success = false, message = "Chỉ giảng viên mới có thể phản hồi bình luận." });
                }

                _context.BinhLuan.Add(model);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"DEBUG: Comment saved to DB with ID: {model.IdBinhLuan}");
                
                // Lấy thông tin user đầy đủ để broadcast
                var currentUser = await _context.TaiKhoan
                    .FirstOrDefaultAsync(tk => tk.IdTaiKhoan == userId);
                
                // Broadcast qua SignalR với đầy đủ thông tin
                var signalRData = new {
                    id = model.IdBinhLuan,
                    noiDung = model.NoiDung,
                    ngayTao = model.NgayTao,
                    idTaiKhoan = model.IdTaiKhoan,
                    idBinhLuanCha = model.IdBinhLuanCha,
                    idBai = model.IdBai,
                    idThongBao = model.IdThongBao,
                    idLopHocPhan = model.IdLopHocPhan,
                    hoTen = currentUser?.HoTen ?? User.Identity.Name,
                    tenDangNhap = currentUser?.TenDangNhap ?? "",
                    avatar = ProcessAvatarPath(currentUser?.AnhDaiDien),
                    role = userRole
                };
                
                Console.WriteLine($"DEBUG: Broadcasting to SignalR group Class_{model.IdLopHocPhan}");
                await _hubContext.Clients.Group($"Class_{model.IdLopHocPhan}").SendAsync("ReceiveComment", signalRData);
                
                Console.WriteLine($"DEBUG: AddComment completed successfully");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: AddComment error: {ex.Message}");
                Console.WriteLine($"DEBUG: AddComment stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Sửa bình luận
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditComment([FromBody] BinhLuanModels model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var comment = await _context.BinhLuan.FindAsync(model.IdBinhLuan);
            if (comment == null) return NotFound();
            // Chỉ chủ bình luận được sửa
            if (comment.IdTaiKhoan != userId) return Forbid();
            comment.NoiDung = model.NoiDung;
            comment.NgayTao = DateTime.Now;
            await _context.SaveChangesAsync();
            
            // Lấy thông tin user để broadcast
            var currentUser = await _context.TaiKhoan
                .FirstOrDefaultAsync(tk => tk.IdTaiKhoan == userId);
            
            await _hubContext.Clients.Group($"Class_{comment.IdLopHocPhan}").SendAsync("UpdateComment", new {
                id = comment.IdBinhLuan,
                noiDung = comment.NoiDung,
                ngayTao = comment.NgayTao,
                hoTen = currentUser?.HoTen ?? User.Identity.Name,
                tenDangNhap = currentUser?.TenDangNhap ?? "",
                avatar = ProcessAvatarPath(currentUser?.AnhDaiDien),
                role = userRole
            });
            return Json(new { success = true });
        }

        // API: Xóa bình luận
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteComment([FromBody] int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var userRole = User.FindFirstValue(ClaimTypes.Role);
                var comment = await _context.BinhLuan.FindAsync(id);
                
                if (comment == null) return NotFound();
                
                // Sinh viên chỉ xóa được bình luận của mình, giảng viên xóa được của mình và sinh viên
                if (userRole == "Sinhvien" && comment.IdTaiKhoan != userId) return Forbid();
                if (userRole == "Giangvien" && comment.IdTaiKhoan != userId)
                {
                    var owner = await _context.TaiKhoan.FindAsync(comment.IdTaiKhoan);
                    if (owner == null || owner.VaiTro != "Sinhvien") return Forbid();
                }
                
                int classId = comment.IdLopHocPhan;
                Console.WriteLine($"DEBUG: Deleting comment {id} and its children for class {classId}");
                
                // Tìm tất cả bình luận con (phản hồi) trước khi xóa comment cha
                var childComments = await _context.BinhLuan
                    .Where(c => c.IdBinhLuanCha == id)
                    .ToListAsync();
                
                Console.WriteLine($"DEBUG: Found {childComments.Count} child comments to delete");
                
                // Xóa tất cả bình luận con trước và broadcast deletion
                foreach (var child in childComments)
                {
                    Console.WriteLine($"DEBUG: Deleting child comment {child.IdBinhLuan}");
                    _context.BinhLuan.Remove(child);
                    // Broadcast xóa comment con
                    await _hubContext.Clients.Group($"Class_{classId}").SendAsync("DeleteComment", child.IdBinhLuan);
                }
                
                // Sau đó xóa comment cha
                Console.WriteLine($"DEBUG: Deleting parent comment {id}");
                _context.BinhLuan.Remove(comment);
                
                // Lưu changes
                await _context.SaveChangesAsync();
                Console.WriteLine($"DEBUG: Successfully deleted comment {id} and {childComments.Count} children");
                
                // Broadcast xóa comment cha
                await _hubContext.Clients.Group($"Class_{classId}").SendAsync("DeleteComment", id);
                
                return Json(new { 
                    success = true, 
                    message = childComments.Count > 0 ? 
                        $"Đã xóa bình luận và {childComments.Count} phản hồi" : 
                        "Đã xóa bình luận"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: DeleteComment error: {ex.Message}");
                Console.WriteLine($"DEBUG: DeleteComment stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Lỗi xóa bình luận: {ex.Message}" });
            }
        }

        // API: Lấy danh sách bình luận cho bài học
        [HttpGet]
        public async Task<IActionResult> GetComments(int idBai)
        {
            try
            {
                Console.WriteLine($"DEBUG: GetComments called with idBai = {idBai}");
                
                var comments = await _context.BinhLuan
                    .Include(c => c.TaiKhoan)  // Include để load thông tin user
                    .Where(c => c.IdBai == idBai)
                    .OrderBy(c => c.NgayTao)
                    .Select(c => new {
                        id = c.IdBinhLuan,
                        noiDung = c.NoiDung,
                        ngayTao = c.NgayTao,
                        idTaiKhoan = c.IdTaiKhoan,
                        idBinhLuanCha = c.IdBinhLuanCha,
                        idBai = c.IdBai,
                        idThongBao = c.IdThongBao,
                        idLopHocPhan = c.IdLopHocPhan,
                        hoTen = c.TaiKhoan != null ? c.TaiKhoan.HoTen : "Ẩn danh",
                        tenDangNhap = c.TaiKhoan != null ? c.TaiKhoan.TenDangNhap : "",
                        rawAvatar = c.TaiKhoan != null ? c.TaiKhoan.AnhDaiDien : null,
                        role = c.TaiKhoan != null ? c.TaiKhoan.VaiTro : ""
                    })
                    .ToListAsync();
                
                // Xử lý avatar path cho từng comment
                var processedComments = comments.Select(c => new {
                    id = c.id,
                    noiDung = c.noiDung,
                    ngayTao = c.ngayTao,
                    idTaiKhoan = c.idTaiKhoan,
                    idBinhLuanCha = c.idBinhLuanCha,
                    idBai = c.idBai,
                    idThongBao = c.idThongBao,
                    idLopHocPhan = c.idLopHocPhan,
                    hoTen = c.hoTen,
                    tenDangNhap = c.tenDangNhap,
                    avatar = ProcessAvatarPath(c.rawAvatar),
                    role = c.role
                }).ToList();
                    
                Console.WriteLine($"DEBUG: Found {processedComments.Count} comments for idBai = {idBai}");
                
                return Json(new { success = true, data = processedComments });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: GetComments error: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Lấy danh sách bình luận theo lớp học phần
        [HttpGet]
        public async Task<IActionResult> GetCommentsByClass(int idLopHocPhan)
        {
            try
            {
                Console.WriteLine($"DEBUG: GetCommentsByClass called with idLopHocPhan = {idLopHocPhan}");
                
                var comments = await _context.BinhLuan
                    .Include(c => c.TaiKhoan)  // Include để load thông tin user
                    .Where(c => c.IdLopHocPhan == idLopHocPhan)
                    .OrderBy(c => c.NgayTao)
                    .Select(c => new {
                        id = c.IdBinhLuan,
                        noiDung = c.NoiDung,
                        ngayTao = c.NgayTao,
                        idTaiKhoan = c.IdTaiKhoan,
                        idBinhLuanCha = c.IdBinhLuanCha,
                        idBai = c.IdBai,
                        idThongBao = c.IdThongBao,
                        idLopHocPhan = c.IdLopHocPhan,
                        hoTen = c.TaiKhoan != null ? c.TaiKhoan.HoTen : "Ẩn danh",
                        tenDangNhap = c.TaiKhoan != null ? c.TaiKhoan.TenDangNhap : "",
                        rawAvatar = c.TaiKhoan != null ? c.TaiKhoan.AnhDaiDien : null,
                        role = c.TaiKhoan != null ? c.TaiKhoan.VaiTro : ""
                    })
                    .ToListAsync();
                
                // Xử lý avatar path cho từng comment
                var processedComments = comments.Select(c => new {
                    id = c.id,
                    noiDung = c.noiDung,
                    ngayTao = c.ngayTao,
                    idTaiKhoan = c.idTaiKhoan,
                    idBinhLuanCha = c.idBinhLuanCha,
                    idBai = c.idBai,
                    idThongBao = c.idThongBao,
                    idLopHocPhan = c.idLopHocPhan,
                    hoTen = c.hoTen,
                    tenDangNhap = c.tenDangNhap,
                    avatar = ProcessAvatarPath(c.rawAvatar),
                    role = c.role
                }).ToList();
                    
                Console.WriteLine($"DEBUG: Found {processedComments.Count} comments for idLopHocPhan = {idLopHocPhan}");
                
                return Json(new { success = true, data = processedComments });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: GetCommentsByClass error: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string ProcessAvatarPath(string rawAvatar)
        {
            if (string.IsNullOrEmpty(rawAvatar))
            {
                return "/images/avatars/avatar.jpg"; // Fallback mặc định
            }

            // Nếu đường dẫn đã có / ở đầu thì dùng trực tiếp
            if (rawAvatar.StartsWith("/"))
            {
                return rawAvatar;
            }
            // Nếu đường dẫn bắt đầu bằng images/ thì thêm / ở đầu
            else if (rawAvatar.StartsWith("images/"))
            {
                return "/" + rawAvatar;
            }
            // Nếu không có format chuẩn thì thêm prefix
            else
            {
                return "/images/avatars/" + rawAvatar;
            }
        }

        public IActionResult DownloadInviteExcelTemplate()
        {
            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            // Tạo file Excel mẫu với các trường: Email, Tên đăng nhập, Họ tên, Vai trò, Trạng thái
            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Template");
                worksheet.Cells[1, 1].Value = "Email";
                worksheet.Cells[1, 2].Value = "Tên đăng nhập";
                worksheet.Cells[1, 3].Value = "Họ tên";
                worksheet.Cells[1, 4].Value = "Vai trò";
                worksheet.Cells[1, 5].Value = "Trạng thái";
                // Thêm một dòng ví dụ ĐÚNG CHUẨN
                worksheet.Cells[2, 1].Value = "sv01@example.com";
                worksheet.Cells[2, 2].Value = "sv01";
                worksheet.Cells[2, 3].Value = "Nguyễn Văn A";
                worksheet.Cells[2, 4].Value = "Sinhvien"; // ĐÚNG CHUẨN
                worksheet.Cells[2, 5].Value = "HoatDong"; // ĐÚNG CHUẨN
                worksheet.Cells.AutoFitColumns();
                var stream = new System.IO.MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                var fileName = "InviteStudentsTemplate.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        [HttpPost]
        public async Task<IActionResult> PreviewInviteExcel(int idLopHocPhan, IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
                return Json(new { success = false, message = "Vui lòng chọn file Excel" });
            var excelService = new ExcelService();
            var result = excelService.ImportInviteStudentsFromExcel(excelFile.OpenReadStream());
            var valid = new List<dynamic>();
            var invalid = new List<string>(result.Invalid);
            foreach (var row in result.Valid)
            {
                // Tìm sinh viên theo email hoặc tên đăng nhập
                var query = _context.TaiKhoan.AsQueryable();
                if (!string.IsNullOrWhiteSpace(row.email))
                    query = query.Where(tk => tk.Email == row.email);
                else if (!string.IsNullOrWhiteSpace(row.tenDangNhap))
                    query = query.Where(tk => tk.TenDangNhap == row.tenDangNhap);
                var user = await query.FirstOrDefaultAsync();
                if (user == null)
                {
                    invalid.Add($"Không tìm thấy sinh viên với Email '{row.email}' hoặc Tên đăng nhập '{row.tenDangNhap}'");
                    continue;
                }
                if (user.VaiTro != "Sinhvien")
                {
                    invalid.Add($"{user.HoTen} ({user.Email}) không phải là sinh viên");
                    continue;
                }
                if (user.TrangThai != "HoatDong")
                {
                    invalid.Add($"{user.HoTen} ({user.Email}) đang bị khóa hoặc không hoạt động");
                    continue;
                }
                // Kiểm tra đã tham gia lớp chưa
                bool alreadyInClass = await _context.LopHocPhan_SinhVien.AnyAsync(x => x.IdLopHocPhan == idLopHocPhan && x.IdTaiKhoan == user.IdTaiKhoan);
                if (alreadyInClass)
                {
                    invalid.Add($"{user.HoTen} ({user.Email}) đã tham gia lớp này");
                    continue;
                }
                valid.Add(new { email = user.Email, tenDangNhap = user.TenDangNhap, hoTen = user.HoTen, idTaiKhoan = user.IdTaiKhoan });
            }
            return Json(new { success = true, valid, invalid });
        }
        
        [HttpPost]
        public async Task<IActionResult> ConfirmInviteExcel(int idLopHocPhan, IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
                return Json(new { success = false, message = "Vui lòng chọn file Excel" });
            var excelService = new ExcelService();
            var result = excelService.ImportInviteStudentsFromExcel(excelFile.OpenReadStream());
            var validUsers = new List<Lecture_web.Models.TaiKhoanModels>();
            foreach (var row in result.Valid)
            {
                var query = _context.TaiKhoan.AsQueryable();
                if (!string.IsNullOrWhiteSpace(row.email))
                    query = query.Where(tk => tk.Email == row.email);
                else if (!string.IsNullOrWhiteSpace(row.tenDangNhap))
                    query = query.Where(tk => tk.TenDangNhap == row.tenDangNhap);
                var user = await query.FirstOrDefaultAsync();
                if (user == null || user.VaiTro != "Sinhvien" || user.TrangThai != "HoatDong")
                    continue;
                // Kiểm tra đã tham gia lớp chưa
                bool alreadyInClass = await _context.LopHocPhan_SinhVien.AnyAsync(x => x.IdLopHocPhan == idLopHocPhan && x.IdTaiKhoan == user.IdTaiKhoan);
                if (alreadyInClass)
                    continue;
                validUsers.Add(user);
            }
            if (validUsers.Count == 0)
                return Json(new { success = false, message = "Không có sinh viên hợp lệ để mời" });
            // Lấy thông tin lớp học phần
            var lopHocPhan = await _context.LopHocPhan.FindAsync(idLopHocPhan);
            if (lopHocPhan == null)
                return Json(new { success = false, message = "Không tìm thấy lớp học phần" });
            var successfulInvites = new List<string>();
            var failedInvites = new List<string>();
            foreach (var user in validUsers)
            {
                // KHÔNG thêm vào LopHocPhan_SinhVien ở đây!
                // Tạo token mời
                var inviteToken = Guid.NewGuid().ToString();
                var inviteExpiry = DateTime.Now.AddDays(7);
                var invitation = new Lecture_web.Models.ThongBaoModels
                {
                    IdTaiKhoan = user.IdTaiKhoan,
                    IdLopHocPhan = idLopHocPhan,
                    NoiDung = $"INVITE|{inviteToken}|{idLopHocPhan}|Lời mời tham gia lớp: {lopHocPhan.TenLop}",
                    NgayTao = DateTime.Now,
                    NgayCapNhat = DateTime.Now
                };
                _context.ThongBao.Add(invitation);
                // Gửi email mời
                var acceptUrl = $"{Request.Scheme}://{Request.Host}/User/ChiTietHocPhan/AcceptInvitation?token={inviteToken}";
                var emailBody = $@"
                    <h2>Lời mời tham gia lớp học</h2>
                    <p>Chào {user.HoTen},</p>
                    <p>Bạn được mời tham gia lớp học: <strong>{lopHocPhan.TenLop}</strong></p>
                    <p>Mô tả: {lopHocPhan.MoTa ?? "Không có mô tả"}</p>
                    <p>Vui lòng click vào link bên dưới để tham gia:</p>
                    <a href='{acceptUrl}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Tham gia lớp học</a>
                    <p><small>Link này có hiệu lực đến {inviteExpiry:dd/MM/yyyy HH:mm}</small></p>
                ";
                try
                {
                    await _emailService.SendEmailAsync(user.Email, $"Lời mời tham gia lớp {lopHocPhan.TenLop}", emailBody);
                    successfulInvites.Add($"{user.HoTen} ({user.Email})");
                }
                catch (Exception emailEx)
                {
                    failedInvites.Add($"{user.HoTen} ({user.Email}): {emailEx.Message}");
                }
            }
            await _context.SaveChangesAsync();
            var resultMessage = $"Đã gửi lời mời thành công cho {successfulInvites.Count} sinh viên";
            if (failedInvites.Count > 0)
            {
                resultMessage += $"\nLỗi gửi email: {string.Join(", ", failedInvites)}";
            }
            return Json(new { success = true, message = resultMessage });
        }
    }
} 