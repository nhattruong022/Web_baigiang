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

                    await _hubContext.Clients.Group($"Class_{request.IdLopHocPhan}")
                        .SendAsync("NewNotification", notificationData);
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

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Test()
        {
            return Json(new { success = true, message = "ThongBao Controller is working!", timestamp = DateTime.Now });
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

        [HttpPost]
        public async Task<IActionResult> AddSampleNotifications(int idLopHocPhan)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Json(new { success = false, message = "Không thể xác định người dùng" });
                }

                var sampleNotifications = new List<ThongBaoModels>
                {
                    new ThongBaoModels
                    {
                        NoiDung = "**Thông báo về lịch học**\n\nLịch học tuần này sẽ chuyển sang phòng B203. Các bạn chú ý đến đúng giờ!",
                        NgayTao = DateTime.Now.AddDays(-2),
                        NgayCapNhat = DateTime.Now.AddDays(-2),
                        IdTaiKhoan = userId,
                        IdLopHocPhan = idLopHocPhan
                    },
                    new ThongBaoModels
                    {
                        NoiDung = "**Nhắc nhở nộp bài tập**\n\nNhớ nộp bài tập chương 1 trước ngày 10/06. Nếu có thắc mắc liên hệ trợ giảng.",
                        NgayTao = DateTime.Now.AddDays(-1),
                        NgayCapNhat = DateTime.Now.AddDays(-1),
                        IdTaiKhoan = userId,
                        IdLopHocPhan = idLopHocPhan
                    },
                    new ThongBaoModels
                    {
                        NoiDung = "**Lưu ý về việc chia sẻ tài liệu**\n\nCác nhóm lưu ý, không share tài liệu đồ án nhóm mình cho các nhóm khác. Thầy đã cố gắng tạo các tài liệu khác nhau giữa các nhóm.",
                        NgayTao = DateTime.Now,
                        NgayCapNhat = DateTime.Now,
                        IdTaiKhoan = userId,
                        IdLopHocPhan = idLopHocPhan
                    }
                };

                _context.ThongBao.AddRange(sampleNotifications);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Đã thêm {sampleNotifications.Count} thông báo mẫu" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi thêm thông báo mẫu: {ex.Message}" });
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
    }

    public class AddNotificationRequest
    {
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public int IdLopHocPhan { get; set; }
    }

    public class EditNotificationRequest
    {
        public int IdThongBao { get; set; }
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
    }

    public class DeleteNotificationRequest
    {
        public int IdThongBao { get; set; }
    }
} 