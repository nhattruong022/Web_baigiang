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

            // DEBUG: Log ID được truyền vào
            Console.WriteLine($"=== CHITIET HOCPHAN DEBUG ===");
            Console.WriteLine($"Received idLopHocPhan parameter: {idLopHocPhan}");
            Console.WriteLine($"User role: {userRole}");

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

                Console.WriteLine($"Found {allLopHocPhan.Count} total lớp học phần in dropdown");
                foreach (var item in allLopHocPhan.Take(5))
                {
                    Console.WriteLine($"  LHP: ID={item.IdLopHocPhan}, TenLop={item.TenLop}, HocPhan={item.TenHocPhan}");
                }

                // Nếu không có idLopHocPhan, lấy lớp học phần đầu tiên để test
                int targetLopHocPhanId = idLopHocPhan ?? allLopHocPhan.FirstOrDefault()?.IdLopHocPhan ?? 1;
                
                Console.WriteLine($"Target LopHocPhan ID determined: {targetLopHocPhanId} (from parameter: {idLopHocPhan})");

                // Lấy thông tin lớp học phần
                var lopHocPhan = await _context.LopHocPhan
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

                // DEBUG: Kiểm tra raw data trong LopHocPhan_SinhVien trước
                var rawStudentRecords = await _context.LopHocPhan_SinhVien
                    .Where(lhp_sv => lhp_sv.IdLopHocPhan == targetLopHocPhanId)
                    .ToListAsync();
                
                Console.WriteLine($"Raw LopHocPhan_SinhVien records for class {targetLopHocPhanId}: {rawStudentRecords.Count}");
                foreach (var record in rawStudentRecords.Take(5))
                {
                    Console.WriteLine($"  Record: IdLopHocPhan={record.IdLopHocPhan}, IdTaiKhoan={record.IdTaiKhoan}");
                }

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
                ViewBag.IdLopHocPhan = idLopHocPhan ?? 1;
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

        // API để gửi lời mời tham gia lớp học (nhiều sinh viên)
        [HttpPost]
        [Authorize(Roles = "Giangvien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteStudents(int idLopHocPhan, int[] idTaiKhoans)
        {
            try
            {
                // Debug log để kiểm tra dữ liệu đầu vào
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                Console.WriteLine($"DEBUG: idLopHocPhan={idLopHocPhan}, số sinh viên cần mời={idTaiKhoans?.Length ?? 0}, currentUserId={currentUserId}");
                
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
                
                Console.WriteLine($"DEBUG: LopHocPhan found - IdTaiKhoan={lopHocPhan.IdTaiKhoan}, TenLop={lopHocPhan.TenLop}");

                // Lấy thông tin tất cả sinh viên được chọn
                var students = await _context.TaiKhoan
                    .Where(tk => idTaiKhoans.Contains(tk.IdTaiKhoan) && 
                                tk.VaiTro == "Sinhvien" && 
                                tk.TrangThai == "HoatDong")
                    .ToListAsync();

                Console.WriteLine($"DEBUG: Found {students.Count} valid students from {idTaiKhoans.Length} requested IDs");

                // Kiểm tra các sinh viên đã tham gia lớp chưa
                var existingMembers = await _context.LopHocPhan_SinhVien
                    .Where(lhp_sv => lhp_sv.IdLopHocPhan == idLopHocPhan && 
                                    idTaiKhoans.Contains(lhp_sv.IdTaiKhoan))
                    .Select(lhp_sv => lhp_sv.IdTaiKhoan)
                    .ToListAsync();

                Console.WriteLine($"DEBUG: Found {existingMembers.Count} students already in class");

                // Lọc ra những sinh viên chưa tham gia lớp
                var studentsToInvite = students
                    .Where(s => !existingMembers.Contains(s.IdTaiKhoan))
                    .ToList();

                if (studentsToInvite.Count == 0)
                {
                    return Json(new { success = false, message = "Tất cả sinh viên đã tham gia lớp học này rồi" });
                }

                Console.WriteLine($"DEBUG: Will invite {studentsToInvite.Count} students");

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
                            Console.WriteLine($"DEBUG: Email sent successfully to {student.Email}");
                        }
                        catch (Exception emailEx)
                        {
                            Console.WriteLine($"DEBUG: Email sending failed for {student.Email}: {emailEx.Message}");
                            successfulInvites.Add($"{student.HoTen} ({student.Email}) - Email failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"DEBUG: Failed to invite {student.Email}: {ex.Message}");
                        failedInvites.Add($"{student.HoTen} ({student.Email}): {ex.Message}");
                    }
                }

                // Lưu tất cả invitations vào database
                await _context.SaveChangesAsync();
                Console.WriteLine($"DEBUG: All invitations saved to database");

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
                Console.WriteLine($"DEBUG: Exception in InviteStudents: {ex.Message}");
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


