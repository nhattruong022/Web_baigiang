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

namespace Lecture_web.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "Giangvien,Sinhvien")]
    public class ChiTietHocPhanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ChiTietHocPhanController(ApplicationDbContext context, EmailService emailService, IHubContext<NotificationHub> hubContext)
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

            // DEBUG: Log ID được truyền vào
            Console.WriteLine($"=== CHITIET HOCPHAN DEBUG ===");
            Console.WriteLine($"Received idLopHocPhan parameter: {idLopHocPhan}");
            Console.WriteLine($"User role: {userRole}");
            Console.WriteLine($"Current user: {currentUser?.HoTen}, Avatar: {currentUser?.AnhDaiDien}");

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
                    Console.WriteLine($"ERROR: Invalid user role: {userRole}");
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

                Console.WriteLine($"Found {allLopHocPhan.Count} total lớp học phần in dropdown");
                foreach (var item in allLopHocPhan.Take(5))
                {
                    Console.WriteLine($"  LHP: ID={item.IdLopHocPhan}, TenLop={item.TenLop}, HocPhan={item.TenHocPhan}");
                }

                // Kiểm tra nếu có idLopHocPhan được chỉ định
                if (idLopHocPhan.HasValue)
                {
                    // Kiểm tra user có quyền truy cập lớp học này không
                    var hasAccess = allLopHocPhan.Any(lhp => lhp.IdLopHocPhan == idLopHocPhan.Value);
                    if (!hasAccess)
                    {
                        Console.WriteLine($"ERROR: User {currentUserId} ({userRole}) does not have access to class {idLopHocPhan}");
                        Console.WriteLine($"User's accessible classes: {string.Join(", ", allLopHocPhan.Select(lhp => lhp.IdLopHocPhan))}");
                        return Forbid(); // HTTP 403 - Forbidden
                    }
                }

                // Nếu không có idLopHocPhan, lấy lớp học phần đầu tiên mà user có quyền truy cập
                int targetLopHocPhanId = idLopHocPhan ?? allLopHocPhan.FirstOrDefault()?.IdLopHocPhan ?? 0;
                
                if (targetLopHocPhanId == 0)
                {
                    Console.WriteLine($"ERROR: No accessible classes found for user {currentUserId} ({userRole})");
                    ViewBag.ErrorMessage = userRole == "Giangvien" ? 
                        "Bạn chưa tạo lớp học nào. Vui lòng tạo lớp học trước." :
                        "Bạn chưa tham gia lớp học nào. Vui lòng liên hệ giảng viên để được mời vào lớp.";
                    ViewBag.AllLopHocPhan = allLopHocPhan;
                    return View("NoAccess");
                }
                
                Console.WriteLine($"Target LopHocPhan ID determined: {targetLopHocPhanId} (from parameter: {idLopHocPhan})");

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

                // DEBUG: Kiểm tra dữ liệu chương và bài
                Console.WriteLine($"=== CHUONG & BAI DEBUG ===");
                Console.WriteLine($"lopHocPhan.IdLopHocPhan: {lopHocPhan.IdLopHocPhan}");
                Console.WriteLine($"lopHocPhan.TenLop: {lopHocPhan.TenLop}");
                Console.WriteLine($"lopHocPhan.IdBaiGiang: {lopHocPhan.IdBaiGiang}");
                Console.WriteLine($"Query: SELECT * FROM Chuong WHERE IdBaiGiang = {lopHocPhan.IdBaiGiang}");
                Console.WriteLine($"Found {chuongs.Count} chuongs for IdBaiGiang={lopHocPhan.IdBaiGiang}");
                
                // Kiểm tra tất cả Chuong trong database có IdBaiGiang gì
                var allChuongsInDb = await _context.Chuong.Select(c => new { c.IdChuong, c.TenChuong, c.IdBaiGiang }).ToListAsync();
                Console.WriteLine($"=== ALL CHUONGS IN DATABASE ===");
                foreach (var c in allChuongsInDb)
                {
                    Console.WriteLine($"  DB Chuong: ID={c.IdChuong}, TenChuong='{c.TenChuong}', IdBaiGiang={c.IdBaiGiang}");
                }
                
                foreach (var chuong in chuongs)
                {
                    Console.WriteLine($"  Chuong: ID={chuong.IdChuong}, TenChuong='{chuong.TenChuong}', Bais={chuong.Bais.Count}");
                    foreach (var bai in chuong.Bais.Take(3))
                    {
                        Console.WriteLine($"    Bai: ID={bai.IdBai}, TieuDe='{bai.TieuDeBai}'");
                    }
                }

                // Nếu không có dữ liệu thực, tạo dữ liệu demo
                if (!chuongs.Any())
                {
                    Console.WriteLine($"No chuongs found, creating demo data...");
                    var demoChuongs = new[]
                    {
                        new
                        {
                            IdChuong = 1,
                            TenChuong = "Chương 1: Cơ bản về C",
                            NgayTao = DateTime.Now,
                            IdBaiGiang = (int)lopHocPhan.IdBaiGiang,
                            Bais = new[]
                            {
                                new
                                {
                                    IdBai = 1,
                                    TieuDeBai = "Hướng dẫn cài đặt môi trường C",
                                    NoiDungText = "Bài hướng dẫn chi tiết cách cài GCC, thiết lập IDE...",
                                    NgayTao = DateTime.Now,
                                    IdChuong = 1
                                },
                                new
                                {
                                    IdBai = 2,
                                    TieuDeBai = "Cú pháp cơ bản của C",
                                    NoiDungText = "Học về biến, kiểu dữ liệu, toán tử trong C...",
                                    NgayTao = DateTime.Now.AddDays(1),
                                    IdChuong = 1
                                }
                            }.ToList()
                        },
                        new
                        {
                            IdChuong = 2,
                            TenChuong = "Chương 2: Kiến trúc OSI",
                            NgayTao = DateTime.Now.AddDays(7),
                            IdBaiGiang = (int)lopHocPhan.IdBaiGiang,
                            Bais = new[]
                            {
                                new
                                {
                                    IdBai = 3,
                                    TieuDeBai = "7 lớp của mô hình OSI",
                                    NoiDungText = "Giới thiệu 7 lớp: Physical, Data Link, Network...",
                                    NgayTao = DateTime.Now.AddDays(8),
                                    IdChuong = 2
                                }
                            }.ToList()
                        }
                    }.ToList();
                    
                    // Gán lại cho chuongs
                    chuongs = demoChuongs;
                    Console.WriteLine($"Created {chuongs.Count} demo chuongs");
                }

                // DEBUG: Kiểm tra raw data trước
                var rawStudentRecords = await _context.LopHocPhan_SinhVien
                    .Where(lhp_sv => lhp_sv.IdLopHocPhan == targetLopHocPhanId)
                    .ToListAsync();
                
                Console.WriteLine($"Raw LopHocPhan_SinhVien records for class {targetLopHocPhanId}: {rawStudentRecords.Count}");
                foreach (var record in rawStudentRecords.Take(5))
                {
                    Console.WriteLine($"  Record: IdLopHocPhan={record.IdLopHocPhan}, IdTaiKhoan={record.IdTaiKhoan}");
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
                
                // DEBUG: In ra JSON sẽ được gửi sang View
                Console.WriteLine($"=== ViewBag.Chuongs JSON ===");
                try 
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(chuongs, new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                        WriteIndented = true 
                    });
                    Console.WriteLine(jsonString);
                }
                catch (Exception jsonEx)
                {
                    Console.WriteLine($"JSON Serialize Error: {jsonEx.Message}");
                }
                Console.WriteLine($"=== END ViewBag.Chuongs JSON ===");
                
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
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                
                // Trả về thông báo lỗi chi tiết
                ViewBag.ErrorMessage = $"Lỗi hệ thống: {ex.Message}";
                ViewBag.UserRole = userRole;
                ViewBag.AllLopHocPhan = new List<object>();
                return View("NoAccess");
            }
        }

        // API để tìm kiếm sinh viên theo nhiều tiêu chí
        [HttpGet]
        public async Task<IActionResult> SearchStudents(string searchTerm, string searchType = "all")
        {
            try
            {
                Console.WriteLine($"=== SEARCH STUDENTS DEBUG ===");
                Console.WriteLine($"SearchTerm: '{searchTerm}'");
                Console.WriteLine($"SearchType: '{searchType}'");
                
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    Console.WriteLine($"SearchTerm is null or whitespace");
                    return Json(new { success = false, message = "Từ khóa tìm kiếm không được để trống" });
                }

                var query = _context.TaiKhoan
                    .Where(tk => tk.VaiTro == "Sinhvien" && tk.TrangThai == "HoatDong");

                Console.WriteLine($"Base query: SELECT * FROM TaiKhoan WHERE VaiTro = 'Sinhvien' AND TrangThai = 'HoatDong'");

                // Tìm kiếm theo loại
                switch (searchType.ToLower())
                {
                    case "email":
                        query = query.Where(tk => tk.Email.Contains(searchTerm));
                        Console.WriteLine($"Added email filter: Email LIKE '%{searchTerm}%'");
                        break;
                    case "name":
                        query = query.Where(tk => tk.HoTen.Contains(searchTerm));
                        Console.WriteLine($"Added name filter: HoTen LIKE '%{searchTerm}%'");
                        break;
                    case "username":
                        query = query.Where(tk => tk.TenDangNhap.Contains(searchTerm));
                        Console.WriteLine($"Added username filter: TenDangNhap LIKE '%{searchTerm}%'");
                        break;
                    case "phone":
                        query = query.Where(tk => tk.SoDienThoai.Contains(searchTerm));
                        Console.WriteLine($"Added phone filter: SoDienThoai LIKE '%{searchTerm}%'");
                        break;
                    default: // "all"
                        query = query.Where(tk => tk.Email.Contains(searchTerm) || 
                                                 tk.HoTen.Contains(searchTerm) ||
                                                 tk.TenDangNhap.Contains(searchTerm) ||
                                                 (tk.SoDienThoai != null && tk.SoDienThoai.Contains(searchTerm)));
                        Console.WriteLine($"Added 'all' filter: searching in Email, HoTen, TenDangNhap, SoDienThoai");
                        break;
                }

                // First, let's check total students in database
                var totalStudents = await _context.TaiKhoan
                    .Where(tk => tk.VaiTro == "Sinhvien")
                    .CountAsync();
                Console.WriteLine($"Total students in database: {totalStudents}");

                var activeStudents = await _context.TaiKhoan
                    .Where(tk => tk.VaiTro == "Sinhvien" && tk.TrangThai == "HoatDong")
                    .CountAsync();
                Console.WriteLine($"Active students in database: {activeStudents}");

                // Log some sample data
                var sampleStudents = await _context.TaiKhoan
                    .Where(tk => tk.VaiTro == "Sinhvien" && tk.TrangThai == "HoatDong")
                    .Take(5)
                    .Select(tk => new { tk.TenDangNhap, tk.HoTen, tk.Email })
                    .ToListAsync();
                
                Console.WriteLine($"Sample students:");
                foreach (var s in sampleStudents)
                {
                    Console.WriteLine($"  - Username: {s.TenDangNhap}, Name: {s.HoTen}, Email: {s.Email}");
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

                Console.WriteLine($"Found {students.Count} students matching search criteria");
                foreach (var s in students.Take(3))
                {
                    Console.WriteLine($"  - Result: ID={s.IdTaiKhoan}, Username={s.TenDangNhap}, Name={s.HoTen}, Email={s.Email}");
                }

                return Json(new { success = true, data = students });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in SearchStudents: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
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

                // Lấy thông tin sinh viên để log
                var student = await _context.TaiKhoan
                    .FirstOrDefaultAsync(tk => tk.IdTaiKhoan == idTaiKhoan);

                Console.WriteLine($"Removing student {student?.HoTen} (ID: {idTaiKhoan}) from class {idLopHocPhan}");

                // Xóa khỏi lớp
                _context.LopHocPhan_SinhVien.Remove(membership);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Đã xóa sinh viên {student?.HoTen} khỏi lớp"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing student from class: {ex.Message}");
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

        // TEST ACTION - để test API tìm kiếm
        [HttpGet]
        public async Task<IActionResult> DebugAccess()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Lấy teacher classes
            object teacherClasses;
            if (userRole == "Giangvien")
            {
                teacherClasses = await _context.LopHocPhan.Where(lhp => lhp.IdTaiKhoan == currentUserId)
                    .Select(lhp => new { lhp.IdLopHocPhan, lhp.TenLop }).ToListAsync();
            }
            else
            {
                teacherClasses = new List<object>();
            }

            // Lấy student classes
            object studentClasses;
            if (userRole == "Sinhvien")
            {
                studentClasses = await _context.LopHocPhan_SinhVien.Where(sv => sv.IdTaiKhoan == currentUserId)
                    .Select(sv => new { sv.IdLopHocPhan }).ToListAsync();
            }
            else
            {
                studentClasses = new List<object>();
            }

            var result = new
            {
                UserId = currentUserId,
                UserRole = userRole,
                RequestedClass = Request.Query["idLopHocPhan"].ToString(),
                AllClasses = await _context.LopHocPhan.Select(lhp => new { lhp.IdLopHocPhan, lhp.TenLop, lhp.IdTaiKhoan }).ToListAsync(),
                TeacherClasses = teacherClasses,
                StudentClasses = studentClasses
            };

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> TestSearch()
        {
            try
            {
                Console.WriteLine("=== TESTING SEARCH MANUALLY ===");
                
                // Test với "sv01"
                var result = await SearchStudents("sv01", "all");
                Console.WriteLine("Search result for 'sv01' completed");
                
                // Test với một phần họ tên
                var result2 = await SearchStudents("Trương", "name");
                Console.WriteLine("Search result for 'Trương' (name) completed");
                
                return Json(new { message = "Test completed, check console logs" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TestSearch: {ex.Message}");
                return Json(new { error = ex.Message });
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
                    avatar = currentUser?.AnhDaiDien,
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
                avatar = currentUser?.AnhDaiDien,
                role = userRole
            });
            return Json(new { success = true });
        }

        // API: Xóa bình luận
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteComment([FromBody] int id)
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
            _context.BinhLuan.Remove(comment);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.Group($"Class_{classId}").SendAsync("DeleteComment", id);
            return Json(new { success = true });
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
                        avatar = c.TaiKhoan != null ? c.TaiKhoan.AnhDaiDien : null,
                        role = c.TaiKhoan != null ? c.TaiKhoan.VaiTro : ""
                    })
                    .ToListAsync();
                    
                Console.WriteLine($"DEBUG: Found {comments.Count} comments for idBai = {idBai}");
                
                return Json(new { success = true, data = comments });
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
                        avatar = c.TaiKhoan != null ? c.TaiKhoan.AnhDaiDien : null,
                        role = c.TaiKhoan != null ? c.TaiKhoan.VaiTro : ""
                    })
                    .ToListAsync();
                    
                Console.WriteLine($"DEBUG: Found {comments.Count} comments for idLopHocPhan = {idLopHocPhan}");
                
                return Json(new { success = true, data = comments });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: GetCommentsByClass error: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
} 


