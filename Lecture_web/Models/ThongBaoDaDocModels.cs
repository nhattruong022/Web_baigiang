using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Lecture_web.Models
{
    public class ThongBaoDaDocModels
    {
        [Key]
        public int IdThongBao { get; set; }
        public int IdTaiKhoan { get; set; }
        public DateTime NgayDoc { get; set; }
    }
}