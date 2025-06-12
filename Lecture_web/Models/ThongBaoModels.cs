using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Lecture_web.Models
{
    public class ThongBaoModels
    {
        [Key]
        public int IdThongBao { get; set; }
        public string NoiDung { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public int IdTaiKhoan { get; set; }
        public int IdLopHocPhan { get; set; }

        [ForeignKey(nameof(IdTaiKhoan))]
        public TaiKhoanModels TaiKhoan { get; set; }

        [ForeignKey(nameof(IdLopHocPhan))]
        public LopHocPhanModels LopHocPhan { get; set; }

        public ICollection<BinhLuanModels> BinhLuans { get; set; }
    }
}
