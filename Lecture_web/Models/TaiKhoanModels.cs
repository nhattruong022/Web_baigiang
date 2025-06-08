using System;
using System.ComponentModel.DataAnnotations;

namespace Lecture_web.Models
{
    public class TaiKhoanModels
    {
        [Key]
        public int IdTaiKhoan { get; set; }
        public string? TenDangNhap { get; set; }
        public string? MatKhau { get; set; }
        public string? HoTen { get; set; }
        public string? VaiTro { get; set; }
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public string? AnhDaiDien { get; set; }
        public string? TrangThai { get; set; }
        public DateTime? NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }
    }
}