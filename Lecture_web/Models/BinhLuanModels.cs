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
        public int IdBai { get; set; }
        public int? IdThongBao { get; set; }

        public TaiKhoanModels TaiKhoan { get; set; }
        public BinhLuanModels BinhLuanCha { get; set; }
        public BaiModels Bai { get; set; }
        public ThongBaoModels ThongBao { get; set; }
        public ICollection<BinhLuanModels> PhanHoi { get; set; }
    }
}
