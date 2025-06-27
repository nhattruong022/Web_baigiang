using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lecture_web.Models
{
    public class BinhLuanModels
    {
        [Key]
        public int IdBinhLuan { get; set; }
        public string NoiDung { get; set; }
        public DateTime NgayTao { get; set; }
        public int IdTaiKhoan { get; set; }
        public int? IdBinhLuanCha { get; set; }
        public int? IdBai { get; set; }
        public int? IdThongBao { get; set; }

        public int IdLopHocPhan { get; set; }

        [ForeignKey(nameof(IdLopHocPhan))]
        public LopHocPhanModels LopHocPhan { get; set; }

        [ForeignKey(nameof(IdTaiKhoan))]
        public TaiKhoanModels TaiKhoan { get; set; }

        [ForeignKey(nameof(IdBinhLuanCha))]
        public BinhLuanModels BinhLuanCha { get; set; }

        [ForeignKey(nameof(IdBai))]
        public BaiModels Bai { get; set; }

        [ForeignKey(nameof(IdThongBao))]
        public ThongBaoModels ThongBao { get; set; }
        public ICollection<BinhLuanModels> PhanHoi { get; set; }
    }
}
