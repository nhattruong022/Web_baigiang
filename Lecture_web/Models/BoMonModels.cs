using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Lecture_web.Models
{
    public class BoMonModels
    {
        [Key]
        public int IdBoMon { get; set; }
        public string TenBoMon { get; set; }
        public string MoTa { get; set; }
        public int IdKhoa { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }

        [ForeignKey(nameof(IdKhoa))]
        public KhoaModels Khoa { get; set; }
        public ICollection<HocPhanModels> HocPhans { get; set; }
    }
}
