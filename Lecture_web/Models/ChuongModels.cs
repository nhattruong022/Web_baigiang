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
        [MaxLength(100, ErrorMessage = "Tên chương tối đa 100 ký tự")]
        public string TenChuong { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public int IdBaiGiang { get; set; }

        [ForeignKey(nameof(IdBaiGiang))]
        public BaiGiangModels BaiGiang { get; set; }
        public ICollection<BaiModels> Bais { get; set; }
    }
}
