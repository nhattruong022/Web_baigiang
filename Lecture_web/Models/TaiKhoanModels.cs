using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Net.WebRequestMethods;

namespace Lecture_web.Models
{
    public class TaiKhoanModels
    {
        [Key]
        public int IdTaiKhoan { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống"), MaxLength(50)]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống"), MaxLength(255)]
        public string MatKhau { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống"), MaxLength(100)]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vai trò không được để trống"), MaxLength(20)]
        public string VaiTro { get; set; }

        [Required(ErrorMessage = "Email không được để trống"), MaxLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống"), MaxLength(20)]
        public string SoDienThoai { get; set; }

        [MaxLength(255)]
        public string? AnhDaiDien { get; set; }

        [MaxLength(20)]
        public string TrangThai { get; set; }

        public DateTime? NgayTao { get; set; }

        
        public DateTime? NgayCapNhat { get; set; }

        public ICollection<OtpModels>? OTPs { get; set; }
        public ICollection<BaiGiangModels>? BaiGiangs { get; set; }
        public ICollection<LopHocPhanModels>? LopHocPhans { get; set; }
        public ICollection<LopHocPhan_SinhVienModels>? LopHocPhan_SinhViens { get; set; }
        public ICollection<ThongBaoModels>? ThongBaos { get; set; }
        public ICollection<BinhLuanModels>? BinhLuans { get; set; }
    }
}