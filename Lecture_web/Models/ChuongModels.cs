using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Lecture_web.Models
{
    public class ChuongModels
    {
        [Key]
        public int IdChuong { get; set; }
        public string TenChuong { get; set; }
        public string TrangThai { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public int IdBaiGiang { get; set; }

        public BaiGiangModels BaiGiang { get; set; }
        public ICollection<BaiModels> Bais { get; set; }
    }
}
