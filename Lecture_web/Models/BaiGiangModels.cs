using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Lecture_web.Models
{
    public class BaiGiangModels
    {
        [Key]
        public int IdBaiGiang { get; set; }

        public string TieuDe { get; set; }


        public string MoTa { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public int IdTaiKhoan { get; set; }

        [ForeignKey(nameof(IdTaiKhoan))]
        public TaiKhoanModels TaiKhoan { get; set; }
        public ICollection<ChuongModels> Chuongs { get; set; }
        public ICollection<LopHocPhanModels> LopHocPhans { get; set; }
    }
}
