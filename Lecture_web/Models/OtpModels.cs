using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Lecture_web.Models
{
    public class OtpModels
    {
        [Key]
        public int OtpId { get; set; }
        public int IdTaiKhoan { get; set; }
        public string OtpCode { get; set; }
        public DateTime NgayHetHan { get; set; }
        public DateTime NgayTao { get; set; }

        public TaiKhoanModels TaiKhoan { get; set; }
    }
}
