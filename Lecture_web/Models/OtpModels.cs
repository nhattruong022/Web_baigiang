using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Lecture_web.Models
{
    [Table("otp")]
    public class OtpModels
    {
        [Key]
        [Column("otp_id")]
        public int OtpId { get; set; }
        
        [Column("idTaiKhoan")]
        public int IdTaiKhoan { get; set; }
        
        [Column("otp_code")]
        public string OtpCode { get; set; }
        
        [Column("ngayHetHan")]
        public DateTime NgayHetHan { get; set; }
        
        [Column("ngayTao")]
        public DateTime NgayTao { get; set; }

        [ForeignKey("IdTaiKhoan")]
        public TaiKhoanModels TaiKhoan { get; set; }
    }
}
