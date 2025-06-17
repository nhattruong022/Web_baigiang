using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Azure.Core.HttpHeader;

namespace Lecture_web.Models
{
    public class HocPhanModels
    {
        [Key]
        public int IdHocPhan { get; set; }
        public string? TenHocPhan { get; set; }
        public string? MoTa { get; set; }
        public string? TrangThai { get; set; }
        public int? IdBoMon { get; set; }
        public DateTime? NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        [ForeignKey("IdBoMon")]
        public BoMonModels? BoMon { get; set; }
        public ICollection<LopHocPhanModels>? LopHocPhans { get; set; }
    }
}
