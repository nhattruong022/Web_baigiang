using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Lecture_web.Models
{
    public class BoMonModels
    {
        [Key]
        public int? IdBoMon { get; set; }
        [Required]
        [MaxLength(100, ErrorMessage = "Tên bộ môn tối đa 100 ký tự")]
        public string? TenBoMon { get; set; }
        [Required]
        public string? MoTa { get; set; }
        [Required]
        public int? IdKhoa { get; set; }
        public DateTime? NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        [ForeignKey("IdKhoa")]
        public KhoaModels? Khoa { get; set; }
        public ICollection<HocPhanModels>? HocPhans { get; set; }
    }
}
