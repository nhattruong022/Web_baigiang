using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Lecture_web.Models
{
    public class KhoaModels
    {
        [Key]
        public int IdKhoa { get; set; }
        public string TenKhoa { get; set; }
        public string MoTa { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }

        public ICollection<BoMonModels> BoMons { get; set; }
    }
}
