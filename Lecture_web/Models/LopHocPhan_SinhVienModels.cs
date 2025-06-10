using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Lecture_web.Models
{
    public class LopHocPhan_SinhVienModels
    {
        [Key]
        public int IdLopHocPhan { get; set; }
        public int IdTaiKhoan { get; set; }

        public LopHocPhanModels LopHocPhan { get; set; }
        public TaiKhoanModels TaiKhoan { get; set; }
    }
}
