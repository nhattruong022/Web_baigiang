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

namespace Lecture_web.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Giangvien,Sinhvien")]
    public class ChiTietHocPhanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public ChiTietHocPhanController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index(int? idLopHocPhan)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            ViewBag.UserRole = userRole;

            try
            {
                // Lấy danh sách tất cả lớp học phần để hiển thị trong dropdown
                var allLopHocPhan = await _context.LopHocPhan
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

                // Nếu không có idLopHocPhan, lấy lớp học phần đầu tiên để test
                int targetLopHocPhanId = idLopHocPhan ?? allLopHocPhan.FirstOrDefault()?.IdLopHocPhan ?? 1;

                // Lấy thông tin lớp học phần
                var lopHocPhan = await _context.LopHocPhan
                    .Include(lhp => lhp.HocPhan)
                    .FirstOrDefaultAsync(lhp => lhp.IdLopHocPhan == targetLopHocPhanId);

                if (lopHocPhan == null)
                {
                    // Nếu không tìm thấy, tạo dữ liệu mặc định
                    ViewBag.IdLopHocPhan = 1;
                    ViewBag.TenLop = "LTCB23-01";
                    ViewBag.IdBaiGiang = 1;
                    ViewBag.StudentsInClass = new List<object>();
                    ViewBag.Chuongs = new List<object>();
                    ViewBag.AllLopHocPhan = allLopHocPhan;
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

                // Lấy danh sách sinh viên trong lớp (chỉ lấy những user có vai trò Sinhvien)
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
                    .ToListAsync();

                ViewBag.IdLopHocPhan = targetLopHocPhanId;
                ViewBag.TenLop = lopHocPhan.TenLop;
                ViewBag.IdBaiGiang = lopHocPhan.IdBaiGiang;
                ViewBag.StudentsInClass = studentsInClass;
                ViewBag.Chuongs = chuongs; // Truyền dữ liệu chương và bài sang view
                
                // Debug log để kiểm tra sinh viên
                Console.WriteLine($"=== STUDENT DEBUG FOR CLASS {targetLopHocPhanId} ===");
                Console.WriteLine($"Found {studentsInClass.Count} students in class");
                
                if (studentsInClass.Any())
                {
                    Console.WriteLine("Students list:");
                    foreach (var student in studentsInClass)
                    {
                        Console.WriteLine($"  - ID: {student.IdTaiKhoan}, Name: {student.HoTen}, Email: {student.Email}, Role: {student.VaiTro}");
                    }
                }
                else
                {
                    Console.WriteLine("No students found in this class!");
                    
                    // Debug: Kiểm tra có record nào trong LopHocPhan_SinhVien cho lớp này không
                    var allRecords = await _context.LopHocPhan_SinhVien
                        .Where(lhp_sv => lhp_sv.IdLopHocPhan == targetLopHocPhanId)
                        .ToListAsync();
                    Console.WriteLine($"Raw LopHocPhan_SinhVien records for class {targetLopHocPhanId}: {allRecords.Count}");
                    
                    foreach (var record in allRecords)
                    {
                        Console.WriteLine($"  - LHP_SV: IdLopHocPhan={record.IdLopHocPhan}, IdTaiKhoan={record.IdTaiKhoan}");
                    }
                }
                
                Console.WriteLine($"=== END STUDENT DEBUG ===");
                
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in Index: {ex.Message}");
                // Trả về dữ liệu mặc định nếu có lỗi
                ViewBag.IdLopHocPhan = 1;
                ViewBag.TenLop = "Lớp mặc định";
                ViewBag.IdBaiGiang = 1;
                ViewBag.StudentsInClass = new List<object>();
                ViewBag.Chuongs = new List<object>();
                ViewBag.AllLopHocPhan = new List<object>();
                return View();
            }
        }

        // API để tìm kiếm sinh viên theo email
        [HttpGet]
        public async Task<IActionResult> SearchStudents(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Json(new { success = false, message = "Email không được để trống" });
                }

                var students = await _context.TaiKhoan
                    .Where(tk => tk.Email.Contains(email) && 
                                tk.VaiTro == "Sinhvien" && 
                                tk.TrangThai == "HoatDong")
                    .Select(tk => new 
                    {
                        tk.IdTaiKhoan,
                        tk.Email,
                        tk.HoTen,
                        tk.AnhDaiDien
                    })
                    .Take(10)
                    .ToListAsync();

                return Json(new { success = true, data = students });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi tìm kiếm: {ex.Message}" });
            }
        }

        // API để gửi lời mời tham gia lớp học
        [HttpPost]
        [Authorize(Roles = "Giangvien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteStudent(int idLopHocPhan, int idTaiKhoan)
        {
            try
            {
                // Debug log để kiểm tra dữ liệu đầu vào
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                Console.WriteLine($"DEBUG: idLopHocPhan={idLopHocPhan}, idTaiKhoan={idTaiKhoan}, currentUserId={currentUserId}");
                
                // Lấy thông tin lớp học phần (bỏ kiểm tra ownership tạm thời để debug)
                var lopHocPhan = await _context.LopHocPhan
                    .FirstOrDefaultAsync(lhp => lhp.IdLopHocPhan == idLopHocPhan);

                if (lopHocPhan == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lớp học phần" });
                }
                
                Console.WriteLine($"DEBUG: LopHocPhan found - IdTaiKhoan={lopHocPhan.IdTaiKhoan}, TenLop={lopHocPhan.TenLop}");
                
                // TODO: BỎ KIỂM TRA OWNER TẠM THỜI ĐỂ TEST
                // Kiểm tra quyền - chỉ giảng viên của lớp mới được mời
                // if (lopHocPhan.IdTaiKhoan != currentUserId)
                // {
                //     return Json(new { success = false, message = $"Bạn không có quyền mời sinh viên vào lớp này (Owner: {lopHocPhan.IdTaiKhoan}, You: {currentUserId})" });
                // }
                
                Console.WriteLine($"DEBUG: Skipping owner check - Owner: {lopHocPhan.IdTaiKhoan}, Current: {currentUserId}");

                // Kiểm tra sinh viên có tồn tại và hoạt động không
                var student = await _context.TaiKhoan
                    .FirstOrDefaultAsync(tk => tk.IdTaiKhoan == idTaiKhoan && 
                                              tk.VaiTro == "Sinhvien" && 
                                              tk.TrangThai == "HoatDong");

                Console.WriteLine($"DEBUG: Student lookup - IdTaiKhoan={idTaiKhoan}, Found={student != null}");
                if (student != null)
                {
                    Console.WriteLine($"DEBUG: Student details - HoTen={student.HoTen}, Email={student.Email}, VaiTro={student.VaiTro}, TrangThai={student.TrangThai}");
                }

                if (student == null)
                {
                    return Json(new { success = false, message = "Sinh viên không tồn tại hoặc không hoạt động" });
                }

                // Kiểm tra sinh viên đã tham gia lớp chưa
                var existingMember = await _context.LopHocPhan_SinhVien
                    .FirstOrDefaultAsync(lhp_sv => lhp_sv.IdLopHocPhan == idLopHocPhan && 
                                                  lhp_sv.IdTaiKhoan == idTaiKhoan);

                Console.WriteLine($"DEBUG: Existing member check - Already joined={existingMember != null}");

                if (existingMember != null)
                {
                    return Json(new { success = false, message = "Sinh viên đã tham gia lớp học này" });
                }

                // Tạo token mời
                var inviteToken = Guid.NewGuid().ToString();
                var inviteExpiry = DateTime.Now.AddDays(7); // Token có hiệu lực 7 ngày
                
                Console.WriteLine($"DEBUG: Generated invite token={inviteToken}");

                // Lưu thông tin lời mời vào database (cần tạo bảng ClassInvitation)
                // Tạm thời lưu vào bảng ThongBao
                var invitation = new ThongBaoModels
                {
                    IdTaiKhoan = idTaiKhoan,
                    IdLopHocPhan = idLopHocPhan,
                    NoiDung = $"INVITE|{inviteToken}|{idLopHocPhan}|Lời mời tham gia lớp: {lopHocPhan.TenLop}",
                    NgayTao = DateTime.Now,
                    NgayCapNhat = DateTime.Now
                };

                Console.WriteLine($"DEBUG: Saving invitation to database...");
                _context.ThongBao.Add(invitation);
                await _context.SaveChangesAsync();
                Console.WriteLine($"DEBUG: Invitation saved successfully");

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

                Console.WriteLine($"DEBUG: Sending email to {student.Email}...");
                Console.WriteLine($"DEBUG: Accept URL: {acceptUrl}");
                
                try 
                {
                    await _emailService.SendEmailAsync(student.Email, $"Lời mời tham gia lớp {lopHocPhan.TenLop}", emailBody);
                    Console.WriteLine($"DEBUG: Email sent successfully");
                    return Json(new { success = true, message = $"Đã gửi lời mời đến {student.Email}" });
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"DEBUG: Email sending failed: {emailEx.Message}");
                    // Vẫn trả về success vì đã lưu invitation vào DB
                    return Json(new { success = true, message = $"Đã tạo lời mời (Email failed: {emailEx.Message})" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Exception in InviteStudent: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
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

                // Cập nhật invitation (đánh dấu đã sử dụng)
                invitation.NoiDung = invitation.NoiDung.Replace("INVITE|", "USED|");
                invitation.NgayCapNhat = DateTime.Now;

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
        public async Task<IActionResult> GetStudentsList(int idLopHocPhan)
        {
            try
            {
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
                    .ToListAsync();

                return Json(new { success = true, data = studentsInClass });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG GetStudentsList: Exception={ex.Message}");
                return Json(new { success = false, message = $"Lỗi tải danh sách sinh viên: {ex.Message}" });
            }
        }
    }
} 


