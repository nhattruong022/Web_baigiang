using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Lecture_web.Models;
using Lecture_web.Hubs;

namespace Lecture_web.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class ThongBaoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ThongBaoController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> AddNotification([FromBody] AddNotificationRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, message = "Không thể xác định người dùng" });
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(request.TieuDe) || string.IsNullOrWhiteSpace(request.NoiDung))
                {
                    return Json(new { success = false, message = "Tiêu đề và nội dung không được để trống" });
                }

                if (request.IdLopHocPhan <= 0)
                {
                    return Json(new { success = false, message = "Lớp học phần không hợp lệ" });
                }

                // Kiểm tra quyền truy cập lớp học phần
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (userRole != "Giangvien")
                {
                    return Json(new { success = false, message = "Bạn không có quyền thêm thông báo cho lớp học này" });
                }

                // Tạo thông báo mới
                var thongBao = new ThongBaoModels
                {
                    NoiDung = $"**{request.TieuDe}**\n\n{request.NoiDung}",
                    NgayTao = DateTime.Now,
                    NgayCapNhat = DateTime.Now,
                    IdTaiKhoan = userId,
                    IdLopHocPhan = request.IdLopHocPhan
                };

                _context.ThongBao.Add(thongBao);
                await _context.SaveChangesAsync();

                // Lấy thông tin chi tiết để gửi notification
                var thongBaoDetail = await _context.ThongBao
                    .Include(tb => tb.TaiKhoan)
                    .Include(tb => tb.LopHocPhan)
                    .FirstOrDefaultAsync(tb => tb.IdThongBao == thongBao.IdThongBao);

                if (thongBaoDetail != null)
                {
                    // Gửi thông báo real-time cho tất cả sinh viên trong lớp
                    var notificationData = new
                    {
                        IdThongBao = thongBaoDetail.IdThongBao,
                        TieuDe = request.TieuDe,
                        NoiDung = request.NoiDung,
                        NgayTao = thongBaoDetail.NgayTao.ToString("dd/MM/yyyy HH:mm"),
                        TenGiangVien = thongBaoDetail.TaiKhoan.HoTen,
                        TenLopHocPhan = thongBaoDetail.LopHocPhan.TenLop,
                        Avatar = ProcessAvatarPath(thongBaoDetail.TaiKhoan.AnhDaiDien)
                    };

                    // Gửi thông báo cho tất cả sinh viên trong lớp học phần
                    await _hubContext.Clients.Group($"Class_{request.IdLopHocPhan}")
                        .SendAsync("NewNotification", notificationData);

                    // Lấy danh sách sinh viên trong lớp để gửi thông báo riêng
                    var studentsInClass = await _context.LopHocPhan_SinhVien
                        .Where(lhps => lhps.IdLopHocPhan == request.IdLopHocPhan)
                        .Select(lhps => lhps.IdTaiKhoan.ToString())
                        .ToListAsync();

                    // Gửi thông báo cho từng sinh viên
                    foreach (var studentId in studentsInClass)
                    {
                        await _hubContext.Clients.Group($"User_{studentId}")
                            .SendAsync("NewNotification", notificationData);
                    }
                }

                return Json(new { 
                    success = true, 
                    message = "Thêm thông báo thành công!",
                    data = new {
                        IdThongBao = thongBao.IdThongBao,
                        TieuDe = request.TieuDe,
                        NoiDung = request.NoiDung,
                        NgayTao = thongBao.NgayTao.ToString("dd/MM/yyyy HH:mm")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi thêm thông báo: {ex.Message}" });
            }
        }



        [HttpPost]
        public async Task<IActionResult> CleanUpNotifications()
        {
            try
            {
                // Xóa tất cả invitation tokens từ bảng ThongBao
                var invalidNotifications = await _context.ThongBao
                    .Where(tb => tb.NoiDung.StartsWith("USED|") || 
                                tb.NoiDung.StartsWith("INVITE|") ||
                                string.IsNullOrEmpty(tb.NoiDung))
                    .ToListAsync();

                if (invalidNotifications.Any())
                {
                    _context.ThongBao.RemoveRange(invalidNotifications);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = $"Đã xóa {invalidNotifications.Count} bản ghi không hợp lệ" });
                }

                return Json(new { success = true, message = "Dữ liệu đã sạch" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi làm sạch dữ liệu: {ex.Message}" });
            }
        }



        [HttpGet]
        public async Task<IActionResult> GetNotifications(int idLopHocPhan, int page = 1, int pageSize = 10)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetNotifications called - IdLopHocPhan: {idLopHocPhan}, Page: {page}, PageSize: {pageSize}");
                System.Diagnostics.Debug.WriteLine($"User: {User.Identity.Name}, IsAuthenticated: {User.Identity.IsAuthenticated}");

                if (idLopHocPhan <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid IdLopHocPhan");
                    return Json(new { success = false, message = "ID lớp học phần không hợp lệ" });
                }

                var query = _context.ThongBao
                    .Include(tb => tb.TaiKhoan)
                    .Where(tb => tb.IdLopHocPhan == idLopHocPhan && 
                                !tb.NoiDung.StartsWith("USED|") && 
                                !tb.NoiDung.StartsWith("INVITE|") &&
                                !string.IsNullOrEmpty(tb.NoiDung) &&
                                tb.NoiDung.Length > 5) // Thông báo phải có độ dài tối thiểu
                    .OrderByDescending(tb => tb.NgayTao);

                System.Diagnostics.Debug.WriteLine($"Query created for IdLopHocPhan: {idLopHocPhan}");

                var totalNotifications = await query.CountAsync();
                System.Diagnostics.Debug.WriteLine($"Total notifications found: {totalNotifications}");

                var notifications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(tb => new
                    {
                        IdThongBao = tb.IdThongBao,
                        NoiDung = tb.NoiDung,
                        NgayTao = tb.NgayTao.ToString("dd/MM/yyyy HH:mm"),
                        TenGiangVien = tb.TaiKhoan.HoTen,
                        rawAvatar = tb.TaiKhoan.AnhDaiDien
                    })
                    .ToListAsync();

                // Xử lý avatar path cho từng notification
                var processedNotifications = notifications.Select(n => new
                {
                    idThongBao = n.IdThongBao,
                    noiDung = n.NoiDung,
                    ngayTao = n.NgayTao,
                    tenGiangVien = n.TenGiangVien,
                    avatar = ProcessAvatarPath(n.rawAvatar)
                }).ToList();

                System.Diagnostics.Debug.WriteLine($"Notifications retrieved: {processedNotifications.Count}");

                return Json(new
                {
                    success = true,
                    data = processedNotifications,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalNotifications = totalNotifications,
                        totalPages = (int)Math.Ceiling((double)totalNotifications / pageSize),
                        hasNextPage = page < Math.Ceiling((double)totalNotifications / pageSize),
                        hasPreviousPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in GetNotifications: {ex}");
                return Json(new { success = false, message = $"Lỗi khi tải thông báo: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugNotifications(int idLopHocPhan)
        {
            try
            {
                // Lấy tất cả thông báo trong bảng ThongBao cho lớp này
                var allNotifications = await _context.ThongBao
                    .Include(tb => tb.TaiKhoan)
                    .Where(tb => tb.IdLopHocPhan == idLopHocPhan)
                    .OrderByDescending(tb => tb.NgayTao)
                    .Select(tb => new
                    {
                        IdThongBao = tb.IdThongBao,
                        NoiDung = tb.NoiDung,
                        NgayTao = tb.NgayTao.ToString("dd/MM/yyyy HH:mm"),
                        TenGiangVien = tb.TaiKhoan.HoTen,
                        rawAvatar = tb.TaiKhoan.AnhDaiDien,
                        IsInviteToken = tb.NoiDung.StartsWith("USED|") || tb.NoiDung.StartsWith("INVITE|"),
                        IsEmpty = string.IsNullOrEmpty(tb.NoiDung),
                        Length = tb.NoiDung != null ? tb.NoiDung.Length : 0
                    })
                    .ToListAsync();

                // Xử lý avatar path cho từng notification
                var processedNotifications = allNotifications.Select(n => new
                {
                    idThongBao = n.IdThongBao,
                    noiDung = n.NoiDung,
                    ngayTao = n.NgayTao,
                    tenGiangVien = n.TenGiangVien,
                    avatar = ProcessAvatarPath(n.rawAvatar),
                    isInviteToken = n.IsInviteToken,
                    isEmpty = n.IsEmpty,
                    length = n.Length
                }).ToList();

                return Json(new
                {
                    success = true,
                    message = "Debug data retrieved",
                    idLopHocPhan = idLopHocPhan,
                    totalRecords = processedNotifications.Count,
                    data = processedNotifications
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Debug error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditNotification([FromBody] EditNotificationRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, message = "Không thể xác định người dùng" });
                }
                if (string.IsNullOrWhiteSpace(request.TieuDe) || string.IsNullOrWhiteSpace(request.NoiDung))
                {
                    return Json(new { success = false, message = "Tiêu đề và nội dung không được để trống" });
                }
                var thongBao = await _context.ThongBao.FindAsync(request.IdThongBao);
                if (thongBao == null)
                    return Json(new { success = false, message = "Không tìm thấy thông báo" });
                // Kiểm tra quyền: chỉ cho phép sửa nếu là giáo viên tạo thông báo hoặc admin
                if (thongBao.IdTaiKhoan != userId)
                    return Json(new { success = false, message = "Bạn không có quyền sửa thông báo này" });
                thongBao.NoiDung = $"**{request.TieuDe}**\n\n{request.NoiDung}";
                thongBao.NgayCapNhat = DateTime.Now;
                await _context.SaveChangesAsync();
                // Lấy lại chi tiết để gửi realtime
                var thongBaoDetail = await _context.ThongBao.Include(tb => tb.TaiKhoan).Include(tb => tb.LopHocPhan).FirstOrDefaultAsync(tb => tb.IdThongBao == thongBao.IdThongBao);
                if (thongBaoDetail != null)
                {
                    var notificationData = new
                    {
                        IdThongBao = thongBaoDetail.IdThongBao,
                        TieuDe = request.TieuDe,
                        NoiDung = request.NoiDung,
                        NgayTao = thongBaoDetail.NgayTao.ToString("dd/MM/yyyy HH:mm"),
                        TenGiangVien = thongBaoDetail.TaiKhoan.HoTen,
                        TenLopHocPhan = thongBaoDetail.LopHocPhan.TenLop,
                        Avatar = ProcessAvatarPath(thongBaoDetail.TaiKhoan.AnhDaiDien)
                    };
                    await _hubContext.Clients.Group($"Class_{thongBaoDetail.IdLopHocPhan}")
                        .SendAsync("UpdateNotification", notificationData);
                }
                return Json(new { success = true, message = "Cập nhật thông báo thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi sửa thông báo: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNotification([FromBody] DeleteNotificationRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, message = "Không thể xác định người dùng" });
                }

                if (request.IdThongBao <= 0)
                {
                    return Json(new { success = false, message = "ID thông báo không hợp lệ" });
                }

                // Tìm thông báo
                var thongBao = await _context.ThongBao
                    .Include(tb => tb.LopHocPhan)
                    .FirstOrDefaultAsync(tb => tb.IdThongBao == request.IdThongBao);
                    
                if (thongBao == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông báo" });
                }

                // Kiểm tra quyền: chỉ cho phép xóa nếi là giáo viên tạo thông báo
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (userRole != "Giangvien" || thongBao.IdTaiKhoan != userId)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xóa thông báo này" });
                }

                // Lưu thông tin để gửi SignalR
                var classId = thongBao.IdLopHocPhan;

                // Xóa thông báo khỏi database
                _context.ThongBao.Remove(thongBao);
                await _context.SaveChangesAsync();

                // Gửi SignalR để thông báo cho các client khác
                await _hubContext.Clients.Group($"Class_{classId}")
                    .SendAsync("DeleteNotification", request.IdThongBao);

                return Json(new { success = true, message = "Xóa thông báo thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi xóa thông báo: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, message = "Không thể xác định người dùng" });
                }
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Sinhvien")
                {
                    return Json(new { success = false, message = "Chỉ sinh viên mới có thể xem thông báo" });
                }
                
                var userClasses = await _context.LopHocPhan_SinhVien
                    .Where(lhs => lhs.IdTaiKhoan == userId)
                    .ToListAsync();
                    
                if (!userClasses.Any())
                {
                    Console.WriteLine($"DEBUG: User {userId} has no classes");
                    return Json(new { success = true, count = 0 });
                }
                
                var allNotifications = await _context.ThongBao
                    .Where(tb => userClasses.Select(x => x.IdLopHocPhan).Contains(tb.IdLopHocPhan) &&
                                !tb.NoiDung.StartsWith("USED|") &&
                                !tb.NoiDung.StartsWith("INVITE|") &&
                                !string.IsNullOrEmpty(tb.NoiDung) &&
                                tb.NoiDung.Length > 5)
                    .Select(tb => new { tb.IdThongBao, tb.IdLopHocPhan })
                    .ToListAsync();
                    
                int unreadCount = 0;
                foreach (var tb in allNotifications)
                {
                    var userClass = userClasses.FirstOrDefault(x => x.IdLopHocPhan == tb.IdLopHocPhan);
                    if (userClass == null) continue;
                    
                    // Đảm bảo ThongBaoDaDocIds không null
                    var currentReadIds = userClass.ThongBaoDaDocIds ?? "";
                    var readIds = currentReadIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                        
                    if (!readIds.Contains(tb.IdThongBao.ToString())) 
                    {
                        unreadCount++;
                    }
                }
                
                Console.WriteLine($"DEBUG: User {userId} has {unreadCount} unread notifications out of {allNotifications.Count} total");
                return Json(new { success = true, count = unreadCount });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetUnreadCount: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, message = "Không thể xác định người dùng" });
                }
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Sinhvien")
                {
                    return Json(new { success = false, message = "Chỉ sinh viên mới có thể đánh dấu đã đọc" });
                }
                
                var userClasses = await _context.LopHocPhan_SinhVien
                    .Where(lhs => lhs.IdTaiKhoan == userId)
                    .ToListAsync();
                    
                if (!userClasses.Any())
                {
                    return Json(new { success = true, message = "Không có lớp học phần nào" });
                }
                
                var allNotificationIds = await _context.ThongBao
                    .Where(tb => userClasses.Select(x => x.IdLopHocPhan).Contains(tb.IdLopHocPhan) &&
                                !tb.NoiDung.StartsWith("USED|") &&
                                !tb.NoiDung.StartsWith("INVITE|") &&
                                !string.IsNullOrEmpty(tb.NoiDung) &&
                                tb.NoiDung.Length > 5)
                    .Select(tb => new { tb.IdThongBao, tb.IdLopHocPhan })
                    .ToListAsync();
                    
                int totalMarked = 0;
                foreach (var userClass in userClasses)
                {
                    // Đảm bảo ThongBaoDaDocIds không null
                    var currentReadIds = userClass.ThongBaoDaDocIds ?? "";
                    var ids = currentReadIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToHashSet();
                        
                    var classNotificationIds = allNotificationIds
                        .Where(x => x.IdLopHocPhan == userClass.IdLopHocPhan)
                        .Select(x => x.IdThongBao.ToString());
                        
                    foreach (var id in classNotificationIds)
                    {
                        if (!ids.Contains(id))
                        {
                            ids.Add(id);
                            totalMarked++;
                        }
                    }
                    
                    userClass.ThongBaoDaDocIds = string.Join(",", ids);
                    Console.WriteLine($"DEBUG: Updated class {userClass.IdLopHocPhan} with {ids.Count} read notifications");
                }
                
                await _context.SaveChangesAsync();
                Console.WriteLine($"DEBUG: Marked {totalMarked} notifications as read for user {userId}");
                
                return Json(new { success = true, message = $"Đã đánh dấu {totalMarked} thông báo là đã đọc" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in MarkAllAsRead: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)) 
                {
                    return Json(new { success = false, message = "Không thể xác định người dùng" });
                }

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Sinhvien")
                {
                    return Json(new { success = false, message = "Chỉ sinh viên mới có thể đánh dấu đã đọc" });
                }

                var userClasses = await _context.LopHocPhan_SinhVien
                    .Where(x => x.IdTaiKhoan == userId)
                    .ToListAsync();

                bool updated = false;
                foreach (var userClass in userClasses)
                {
                    var notification = await _context.ThongBao
                        .FirstOrDefaultAsync(tb => tb.IdThongBao == id && tb.IdLopHocPhan == userClass.IdLopHocPhan);
                    
                    if (notification != null)
                    {
                        // Đảm bảo ThongBaoDaDocIds không null
                        var currentReadIds = userClass.ThongBaoDaDocIds ?? "";
                        var ids = currentReadIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToHashSet();
                        
                        if (!ids.Contains(id.ToString()))
                        {
                            ids.Add(id.ToString());
                            userClass.ThongBaoDaDocIds = string.Join(",", ids);
                            updated = true;
                            
                            Console.WriteLine($"DEBUG: Marked notification {id} as read for user {userId} in class {userClass.IdLopHocPhan}");
                        }
                    }
                }
                
                if (updated) 
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"DEBUG: Saved changes to database for user {userId}");
                }
                
                return Json(new { success = true, message = "Đã đánh dấu thông báo là đã đọc" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in MarkAsRead: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentNotifications(int limit = 10)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, message = "Không thể xác định người dùng" });
                }

                // Chỉ sinh viên mới có notification list
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Sinhvien")
                {
                    return Json(new { success = true, notifications = new List<object>(), message = "Chỉ sinh viên mới có thông báo" });
                }

                // Lấy tất cả lớp học phần mà sinh viên tham gia
                var userClasses = await _context.LopHocPhan_SinhVien
                    .Where(lhps => lhps.IdTaiKhoan == userId)
                    .Select(lhps => lhps.IdLopHocPhan)
                    .ToListAsync();

                // Lấy thông báo mới nhất từ giảng viên
                var recentNotifications = await _context.ThongBao
                    .Include(tb => tb.TaiKhoan)
                    .Include(tb => tb.LopHocPhan)
                        .ThenInclude(lhp => lhp.HocPhan)
                    .Where(tb => userClasses.Contains(tb.IdLopHocPhan) &&
                                !tb.NoiDung.StartsWith("USED|") &&
                                !tb.NoiDung.StartsWith("INVITE|") &&
                                !string.IsNullOrEmpty(tb.NoiDung) &&
                                tb.NoiDung.Length > 5)
                    .OrderByDescending(tb => tb.NgayTao)
                    .Take(limit)
                    .Select(tb => new
                    {
                        IdThongBao = tb.IdThongBao,
                        NoiDung = tb.NoiDung,
                        NgayTao = tb.NgayTao,
                        TenGiangVien = tb.TaiKhoan.HoTen,
                        TenLopHocPhan = tb.LopHocPhan.TenLop,
                        TenHocPhan = tb.LopHocPhan.HocPhan.TenHocPhan, // Lấy tên học phần từ bảng HocPhan
                        IdLopHocPhan = tb.IdLopHocPhan,
                        RawAvatar = tb.TaiKhoan.AnhDaiDien // Lấy raw avatar, không gọi hàm C#
                    })
                    .ToListAsync();

                var processedNotifications = recentNotifications.Select(n => new
                {
                    idThongBao = n.IdThongBao,
                    noiDung = n.NoiDung,
                    ngayTao = n.NgayTao.ToString("dd/MM/yyyy HH:mm"),
                    tenGiangVien = n.TenGiangVien,
                    tenLopHocPhan = n.TenLopHocPhan,
                    tenHocPhan = n.TenHocPhan, // Thêm tên học phần
                    idLopHocPhan = n.IdLopHocPhan,
                    avatar = ProcessAvatarPath(n.RawAvatar), // Gọi hàm C# ở đây
                    timeAgo = GetTimeAgo(n.NgayTao)
                }).ToList();

                return Json(new { 
                    success = true, 
                    notifications = processedNotifications,
                    message = "Lấy danh sách thông báo thành công"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi lấy danh sách thông báo: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserClasses()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, message = "Không thể xác định người dùng" });
                }

                // Chỉ sinh viên mới có danh sách lớp học phần
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Sinhvien")
                {
                    return Json(new { success = true, classes = new List<object>(), message = "Chỉ sinh viên mới có danh sách lớp" });
                }

                // Lấy tất cả lớp học phần mà sinh viên tham gia
                var userClasses = await _context.LopHocPhan_SinhVien
                    .Include(lhps => lhps.LopHocPhan)
                    .Where(lhps => lhps.IdTaiKhoan == userId)
                    .Select(lhps => new
                    {
                        IdLopHocPhan = lhps.IdLopHocPhan,
                        TenLop = lhps.LopHocPhan.TenLop
                    })
                    .ToListAsync();

                return Json(new { 
                    success = true, 
                    classes = userClasses,
                    message = "Lấy danh sách lớp học phần thành công"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi lấy danh sách lớp: {ex.Message}" });
            }
        }

        private string ProcessAvatarPath(string? rawAvatar)
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

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;
            
            if (timeSpan.TotalDays >= 1)
            {
                return $"{(int)timeSpan.TotalDays} ngày trước";
            }
            else if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours} giờ trước";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            }
            else
            {
                return "Vừa xong";
            }
        }
    }

    public class AddNotificationRequest
    {
        public string TieuDe { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public int IdLopHocPhan { get; set; }
    }

    public class EditNotificationRequest
    {
        public int IdThongBao { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
    }

    public class DeleteNotificationRequest
    {
        public int IdThongBao { get; set; }
    }
} 