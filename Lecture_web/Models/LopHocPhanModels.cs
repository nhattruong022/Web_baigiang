using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Lecture_web.Models
{
    public class LopHocPhanModels
    {
        [Key]
        public int IdLopHocPhan { get; set; }
        public string TenLop { get; set; }
        public string? MoTa { get; set; }
        public string TrangThai { get; set; }
        public int IdHocPhan { get; set; }
        public int IdTaiKhoan { get; set; }
        public int? IdBaiGiang { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }

        [ForeignKey(nameof(IdHocPhan))]
        public HocPhanModels HocPhan { get; set; }

        [ForeignKey(nameof(IdTaiKhoan))]
        public TaiKhoanModels TaiKhoan { get; set; }

        [ForeignKey(nameof(IdBaiGiang))]
        public BaiGiangModels? BaiGiang { get; set; }
        public ICollection<LopHocPhan_SinhVienModels> LopHocPhan_SinhViens { get; set; }
        public ICollection<ThongBaoModels> ThongBaos { get; set; }

    }
}
