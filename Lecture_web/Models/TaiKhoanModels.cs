using System;
using System.ComponentModel.DataAnnotations;

namespace Lecture_web.Models
{
    public class TaiKhoanModels
    {
        [Key]
        public int IdTaiKhoan { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string? TenDangNhap { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string? MatKhau { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        public string? HoTen { get; set; }

        [Required(ErrorMessage = "Vai trò không được để trống")]
        public string? VaiTro { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        public string? SoDienThoai { get; set; }

        
        public string? AnhDaiDien { get; set; }

        public string? TrangThai { get; set; }

        public DateTime? NgayTao { get; set; }

        
        public DateTime? NgayCapNhat { get; set; }
    }
}