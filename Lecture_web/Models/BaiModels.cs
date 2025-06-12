using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Lecture_web.Models
{
    public class BaiModels
    {
        [Key]
        public int IdBai { get; set; }
        public string TieuDeBai { get; set; }
        public string NoiDungText { get; set; }
        public string TrangThai { get; set; }
        public DateTime NgayTao { get; set; }
        public int IdChuong { get; set; }

        [ForeignKey(nameof(IdChuong))]
        public ChuongModels Chuong { get; set; }
        public ICollection<BinhLuanModels> BinhLuans { get; set; }
    }
}
